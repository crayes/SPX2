using System.Net.Http.Json;
using System.Text.Json;

namespace Spx.DeltaWorker.Infrastructure.Graph;

/// <summary>
/// Atualiza campos de metadados nos itens do SharePoint via Microsoft Graph API.
/// </summary>
public sealed class SharePointFieldsUpdater(
    ILogger<SharePointFieldsUpdater> logger,
    GraphApiClient graph)
{
    /// <summary>
    /// Atualiza os campos de metadados de um item no SharePoint.
    /// </summary>
    /// <param name="driveId">ID do drive</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="fields">Campos a serem atualizados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se atualização foi bem sucedida</returns>
    public async Task<bool> UpdateFieldsAsync(
        string driveId,
        string itemId,
        Dictionary<string, object> fields,
        CancellationToken cancellationToken)
    {
        // Endpoint: PATCH /drives/{driveId}/items/{itemId}/listItem/fields
        var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}/listItem/fields";

        try
        {
            var success = await graph.PatchJsonAsync(url, fields, cancellationToken);

            if (success)
            {
                logger.LogDebug("Updated {count} fields for item {itemId}", fields.Count, itemId);
            }
            else
            {
                logger.LogWarning("Failed to update fields for item {itemId}", itemId);
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating fields for item {itemId}: {message}", itemId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Verifica quais campos já estão preenchidos e retorna apenas os que precisam ser atualizados.
    /// </summary>
    public Dictionary<string, object> FilterEmptyFields(
        Dictionary<string, object> newFields,
        IReadOnlyDictionary<string, object?> existingFields,
        bool forceUpdate = false)
    {
        if (forceUpdate)
        {
            return newFields;
        }

        var toUpdate = new Dictionary<string, object>();

        foreach (var (key, value) in newFields)
        {
            // Verifica se o campo existe e está preenchido
            if (existingFields.TryGetValue(key, out var existingValue) && !IsEmpty(existingValue))
            {
                logger.LogDebug("Skipping field {field} - already has value", key);
                continue;
            }

            // Campo vazio ou inexistente - adiciona para atualização
            toUpdate[key] = value;
        }

        return toUpdate;
    }

    private static bool IsEmpty(object? value)
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            _ => false
        };
    }
}
