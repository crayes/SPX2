using System.Text.Json;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Configuration;
using Spx.DeltaWorker.Infrastructure.Graph;
using Spx.DeltaWorker.Infrastructure.State;

namespace Spx.DeltaWorker.Application;

public sealed class SharePointDeltaEngine(
    ILogger<SharePointDeltaEngine> logger,
    GraphApiClient graph,
    IMetadataSink sink,
    IDeltaStateStore stateStore,
    SharePointFieldsUpdater fieldsUpdater,
    IOptions<DeltaOptions> deltaOptions,
    IOptions<SharePointOptions> sharePointOptions) : IDeltaEngine
{
    private readonly DeltaOptions _deltaOptions = deltaOptions.Value;
    private readonly SharePointOptions _sharePointOptions = sharePointOptions.Value;

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_deltaOptions.Enabled)
        {
            logger.LogDebug("Delta processing is disabled (Delta:Enabled=false). Skipping tick.");
            return;
        }

        var state = await stateStore.LoadAsync(cancellationToken);

        var siteId = state?.SiteId;
        if (string.IsNullOrWhiteSpace(siteId))
        {
            siteId = await ResolveSiteIdAsync(cancellationToken);
        }

        var driveId = state?.DriveId;
        if (string.IsNullOrWhiteSpace(driveId) || !string.Equals(state?.DriveName, _sharePointOptions.DriveName, StringComparison.OrdinalIgnoreCase))
        {
            driveId = await ResolveDriveIdAsync(siteId, cancellationToken);
        }

        var link = state?.ContinuationLink ?? state?.DeltaLink;
        if (string.IsNullOrWhiteSpace(link))
        {
            link = BuildInitialDeltaUrl(driveId);
        }

        var processed = 0;
        var updated = 0;
        var skipped = 0;
        string? lastNextLink = null;
        string? lastDeltaLink = null;

        while (!string.IsNullOrWhiteSpace(link) && processed < _sharePointOptions.MaxItemsPerRun)
        {
            using var json = await graph.GetJsonAsync(link, cancellationToken);
            var page = GraphModels.ParsePage(json);

            lastNextLink = page.NextLink;
            lastDeltaLink = page.DeltaLink;

            foreach (var item in page.Items)
            {
                if (processed >= _sharePointOptions.MaxItemsPerRun)
                {
                    break;
                }

                // Ignora itens deletados, pastas, ou sem ID
                if (item.IsDeleted || item.IsFolder || !item.IsFile || string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                // Obtém campos existentes do SharePoint
                var (existingFields, itemDetails) = await GetItemFieldsAndDetailsAsync(driveId, item.Id, cancellationToken);

                // Gera os 14 campos de metadados inteligentes
                var generatedMetadata = MetadataGenerator.GerarMetadados(
                    fileName: item.Name,
                    parentPath: item.ParentPath ?? string.Empty,
                    sizeBytes: itemDetails.Size,
                    createdDateTime: itemDetails.CreatedDateTime,
                    lastModifiedDateTime: item.LastModifiedUtc,
                    createdByDisplayName: itemDetails.CreatedByDisplayName);

                // Filtra apenas campos que precisam atualização (não sobrescreve existentes)
                var fieldsToUpdate = fieldsUpdater.FilterEmptyFields(
                    generatedMetadata,
                    existingFields,
                    forceUpdate: _sharePointOptions.ForceUpdate);

                if (fieldsToUpdate.Count > 0)
                {
                    // Atualiza os campos no SharePoint
                    var success = await fieldsUpdater.UpdateFieldsAsync(driveId, item.Id, fieldsToUpdate, cancellationToken);

                    if (success)
                    {
                        updated++;
                        logger.LogInformation("Updated {count} fields: {path}",
                            fieldsToUpdate.Count,
                            string.IsNullOrWhiteSpace(item.ParentPath) ? item.Name : $"{item.ParentPath}/{item.Name}");
                    }
                    else
                    {
                        logger.LogWarning("Failed to update: {path}",
                            string.IsNullOrWhiteSpace(item.ParentPath) ? item.Name : $"{item.ParentPath}/{item.Name}");
                    }
                }
                else
                {
                    skipped++;
                    logger.LogDebug("Skipped (fields already filled): {name}", item.Name);
                }

                // Também grava no sink local (NDJSON) para histórico/debug
                var record = new MetadataRecord(
                    item.Id,
                    item.Name,
                    item.WebUrl,
                    item.ParentPath,
                    item.LastModifiedUtc,
                    existingFields);

                await sink.WriteAsync(record, cancellationToken);
                processed++;
            }

            link = page.NextLink;

            if (string.IsNullOrWhiteSpace(link))
            {
                break;
            }
        }

        // Salva estado para próxima execução
        var newState = new DeltaState(
            ContinuationLink: processed >= _sharePointOptions.MaxItemsPerRun ? lastNextLink : null,
            DeltaLink: processed >= _sharePointOptions.MaxItemsPerRun ? null : lastDeltaLink,
            SiteId: siteId,
            DriveId: driveId,
            DriveName: _sharePointOptions.DriveName,
            SavedAtUtc: DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(newState.ContinuationLink) || !string.IsNullOrWhiteSpace(newState.DeltaLink))
        {
            await stateStore.SaveAsync(newState, cancellationToken);
        }

        logger.LogInformation("Delta tick complete: {processed} processed, {updated} updated, {skipped} skipped.",
            processed, updated, skipped);
    }

    private string BuildInitialDeltaUrl(string driveId)
    {
        var root = string.IsNullOrWhiteSpace(_sharePointOptions.FolderPath)
            ? "root"
            : $"root:/{Uri.EscapeDataString(_sharePointOptions.FolderPath.Trim('/'))}";

        return $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/{root}/delta";
    }

    private async Task<string> ResolveSiteIdAsync(CancellationToken cancellationToken)
    {
        var siteUri = new Uri(_sharePointOptions.SiteUrl);
        var sitePath = siteUri.AbsolutePath.TrimEnd('/');
        var url = $"https://graph.microsoft.com/v1.0/sites/{siteUri.Host}:{sitePath}";

        using var json = await graph.GetJsonAsync(url, cancellationToken);
        var root = json.RootElement;
        var siteId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(siteId))
        {
            throw new InvalidOperationException("Unable to resolve site id from SharePoint site URL.");
        }

        logger.LogInformation("Resolved site ID: {siteId}", siteId);
        return siteId;
    }

    private async Task<string> ResolveDriveIdAsync(string siteId, CancellationToken cancellationToken)
    {
        var url = $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/drives";

        using var json = await graph.GetJsonAsync(url, cancellationToken);
        var root = json.RootElement;

        if (!root.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Unable to list drives for site.");
        }

        foreach (var drive in value.EnumerateArray())
        {
            var name = drive.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (!string.Equals(name, _sharePointOptions.DriveName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var id = drive.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                logger.LogInformation("Resolved drive '{name}' ID: {id}", name, id);
                return id;
            }
        }

        throw new InvalidOperationException($"Drive '{_sharePointOptions.DriveName}' not found on site.");
    }

    private async Task<(IReadOnlyDictionary<string, object?> Fields, ItemDetails Details)> GetItemFieldsAndDetailsAsync(
        string driveId, string itemId, CancellationToken cancellationToken)
    {
        var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}?$expand=listItem($expand=fields)";

        using var json = await graph.GetJsonAsync(url, cancellationToken);
        var root = json.RootElement;

        // Extrai detalhes do item
        var details = new ItemDetails
        {
            Size = root.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0,
            CreatedDateTime = root.TryGetProperty("createdDateTime", out var createdProp)
                ? DateTimeOffset.Parse(createdProp.GetString()!)
                : null,
            CreatedByDisplayName = ExtractCreatedByDisplayName(root)
        };

        // Extrai campos existentes
        var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("listItem", out var listItem) &&
            listItem.ValueKind == JsonValueKind.Object &&
            listItem.TryGetProperty("fields", out var fieldsElement) &&
            fieldsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in fieldsElement.EnumerateObject())
            {
                if (prop.Name.StartsWith('@'))
                {
                    continue;
                }

                fields[prop.Name] = ConvertJson(prop.Value);
            }
        }

        return (fields, details);
    }

    private static string? ExtractCreatedByDisplayName(JsonElement root)
    {
        if (root.TryGetProperty("createdBy", out var createdBy) &&
            createdBy.TryGetProperty("user", out var user))
        {
            if (user.TryGetProperty("displayName", out var displayName))
            {
                return displayName.GetString();
            }

            if (user.TryGetProperty("email", out var email))
            {
                return email.GetString();
            }
        }

        return null;
    }

    private static object? ConvertJson(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var i) => i,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(),
        };

    private record ItemDetails
    {
        public long Size { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
        public string? CreatedByDisplayName { get; init; }
    }
}
