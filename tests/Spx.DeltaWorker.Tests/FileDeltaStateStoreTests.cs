using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Configuration;
using Spx.DeltaWorker.Infrastructure.State;
using Xunit;

namespace Spx.DeltaWorker.Tests;

public sealed class FileDeltaStateStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var temp = Directory.CreateTempSubdirectory("spx2-state-");
        try
        {
            var hostEnv = new TestHostEnvironment(temp.FullName);
            var spOptions = Options.Create(new SharePointOptions
            {
                DeltaStateFile = "state.json",
                OutputNdjsonPath = "out.ndjson",
                HttpTimeoutSeconds = 10,
                DriveName = "Documentos",
                SiteUrl = "https://rfaasp.sharepoint.com/sites/copilot",
                TenantId = "t",
                ClientId = "c",
                ClientSecret = "s"
            });

            var store = new FileDeltaStateStore(hostEnv, spOptions);

            var state = new DeltaState(
                ContinuationLink: "https://graph.microsoft.com/v1.0/next",
                DeltaLink: null,
                SiteId: "site",
                DriveId: "drive",
                DriveName: "Documentos",
                SavedAtUtc: DateTimeOffset.UtcNow);

            await store.SaveAsync(state, CancellationToken.None);
            var loaded = await store.LoadAsync(CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(state.ContinuationLink, loaded!.ContinuationLink);
            Assert.Equal(state.SiteId, loaded.SiteId);
            Assert.Equal(state.DriveId, loaded.DriveId);
            Assert.Equal(state.DriveName, loaded.DriveName);
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}