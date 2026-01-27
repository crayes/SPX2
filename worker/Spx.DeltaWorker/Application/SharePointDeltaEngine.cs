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

                if (item.IsDeleted || item.IsFolder || !item.IsFile || string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                var fields = await GetItemFieldsAsync(driveId, item.Id, cancellationToken);
                var filtered = FilterFields(fields);

                var record = new MetadataRecord(
                    item.Id,
                    item.Name,
                    item.WebUrl,
                    item.ParentPath,
                    item.LastModifiedUtc,
                    filtered);

                await sink.WriteAsync(record, cancellationToken);
                processed++;
            }

            link = page.NextLink;

            if (string.IsNullOrWhiteSpace(link))
            {
                break;
            }
        }

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

        logger.LogInformation("SharePoint delta tick processed {processed} file(s).", processed);
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

        // Graph: GET /sites/{hostname}:/sites/{sitePath}
        var sitePath = siteUri.AbsolutePath.TrimEnd('/');
        var url = $"https://graph.microsoft.com/v1.0/sites/{siteUri.Host}:{sitePath}";

        using var json = await graph.GetJsonAsync(url, cancellationToken);
        var root = json.RootElement;
        var siteId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(siteId))
        {
            throw new InvalidOperationException("Unable to resolve site id from SharePoint site URL.");
        }

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
                return id;
            }
        }

        throw new InvalidOperationException($"Drive '{_sharePointOptions.DriveName}' not found on site.");
    }

    private async Task<Dictionary<string, object?>> GetItemFieldsAsync(string driveId, string itemId, CancellationToken cancellationToken)
    {
        // GET /drives/{driveId}/items/{itemId}?$expand=listItem($expand=fields)
        var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}?$expand=listItem($expand=fields)";

        using var json = await graph.GetJsonAsync(url, cancellationToken);
        var root = json.RootElement;

        if (!root.TryGetProperty("listItem", out var listItem) || listItem.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>();
        }

        if (!listItem.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>();
        }

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in fields.EnumerateObject())
        {
            if (prop.Name.StartsWith('@'))
            {
                continue;
            }

            dict[prop.Name] = ConvertJson(prop.Value);
        }

        return dict;
    }

    private IReadOnlyDictionary<string, object?> FilterFields(Dictionary<string, object?> fields)
    {
        if (_sharePointOptions.IncludeFields.Length == 0)
        {
            return fields;
        }

        var filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _sharePointOptions.IncludeFields)
        {
            if (TryGetField(fields, key, out var value))
            {
                filtered[key] = value;
            }
            else
            {
                logger.LogDebug("SharePoint field '{field}' not found on item. Available fields: {count}.", key, fields.Count);
            }
        }

        return filtered;
    }

    private static bool TryGetField(Dictionary<string, object?> fields, string requestedKey, out object? value)
    {
        if (fields.TryGetValue(requestedKey, out value))
        {
            return true;
        }

        // SharePoint/Graph often exposes internal field names where spaces are encoded.
        // Example display name: "Tipo de Documento" -> internal may be "Tipo_x0020_de_x0020_Documento".
        if (requestedKey.Contains(' '))
        {
            var encodedSpaces = requestedKey.Replace(" ", "_x0020_", StringComparison.Ordinal);
            if (fields.TryGetValue(encodedSpaces, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
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
}