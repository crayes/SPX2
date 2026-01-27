using System.Text.Json;

namespace Spx.DeltaWorker.Infrastructure.Graph;

public static class GraphModels
{
    public static GraphPage ParsePage(JsonDocument json)
    {
        var root = json.RootElement;

        var items = new List<DriveItem>();
        if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in value.EnumerateArray())
            {
                items.Add(DriveItem.Parse(element));
            }
        }

        string? nextLink = root.TryGetProperty("@odata.nextLink", out var n) ? n.GetString() : null;
        string? deltaLink = root.TryGetProperty("@odata.deltaLink", out var d) ? d.GetString() : null;

        return new GraphPage(items, nextLink, deltaLink);
    }

    public sealed record GraphPage(IReadOnlyList<DriveItem> Items, string? NextLink, string? DeltaLink);

    public sealed record DriveItem(
        string Id,
        string Name,
        string? WebUrl,
        string? ParentPath,
        DateTimeOffset? LastModifiedUtc,
        bool IsFile,
        bool IsFolder,
        bool IsDeleted)
    {
        public static DriveItem Parse(JsonElement element)
        {
            var id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var name = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

            var webUrl = element.TryGetProperty("webUrl", out var webUrlProp) ? webUrlProp.GetString() : null;

            DateTimeOffset? lastModified = null;
            if (element.TryGetProperty("lastModifiedDateTime", out var lmdProp)
                && lmdProp.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(lmdProp.GetString(), out var parsed))
            {
                lastModified = parsed.ToUniversalTime();
            }

            string? parentPath = null;
            if (element.TryGetProperty("parentReference", out var parentRef)
                && parentRef.ValueKind == JsonValueKind.Object
                && parentRef.TryGetProperty("path", out var pathProp))
            {
                parentPath = pathProp.GetString();
            }

            var isFile = element.TryGetProperty("file", out _);
            var isFolder = element.TryGetProperty("folder", out _);
            var isDeleted = element.TryGetProperty("deleted", out _);

            return new DriveItem(
                id ?? string.Empty,
                name ?? string.Empty,
                webUrl,
                parentPath,
                lastModified,
                isFile,
                isFolder,
                isDeleted);
        }
    }
}