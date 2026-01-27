using System.Text.Json;
using Spx.DeltaWorker.Infrastructure.Graph;
using Xunit;

namespace Spx.DeltaWorker.Tests;

public sealed class GraphModelsTests
{
    [Fact]
    public void ParsePage_ParsesItemsAndLinks()
    {
        const string payload = """
        {
          "value": [
            {
              "id": "1",
              "name": "file1.docx",
              "webUrl": "https://example/file1",
              "lastModifiedDateTime": "2026-01-26T12:34:56Z",
              "file": { "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
              "parentReference": { "path": "/drives/abc/root:/Documentos" }
            },
            {
              "id": "2",
              "name": "folder",
              "folder": { "childCount": 1 }
            },
            {
              "id": "3",
              "name": "deleted.docx",
              "deleted": {}
            }
          ],
          "@odata.nextLink": "https://graph.microsoft.com/v1.0/next"
        }
        """;

        using var doc = JsonDocument.Parse(payload);
        var page = GraphModels.ParsePage(doc);

        Assert.Equal("https://graph.microsoft.com/v1.0/next", page.NextLink);
        Assert.Null(page.DeltaLink);
        Assert.Equal(3, page.Items.Count);

        var file = page.Items[0];
        Assert.True(file.IsFile);
        Assert.False(file.IsFolder);
        Assert.False(file.IsDeleted);
        Assert.Equal("1", file.Id);
        Assert.Equal("file1.docx", file.Name);
        Assert.Equal("/drives/abc/root:/Documentos", file.ParentPath);
        Assert.Equal(DateTimeOffset.Parse("2026-01-26T12:34:56Z"), file.LastModifiedUtc);

        Assert.True(page.Items[1].IsFolder);
        Assert.True(page.Items[2].IsDeleted);
    }

    [Fact]
    public void ParsePage_ParsesDeltaLink()
    {
        const string payload = """
        {
          "value": [],
          "@odata.deltaLink": "https://graph.microsoft.com/v1.0/delta"
        }
        """;

        using var doc = JsonDocument.Parse(payload);
        var page = GraphModels.ParsePage(doc);

        Assert.Null(page.NextLink);
        Assert.Equal("https://graph.microsoft.com/v1.0/delta", page.DeltaLink);
    }
}