using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Application;
using Spx.DeltaWorker.Configuration;
using Spx.DeltaWorker.Tests.Infrastructure;
using Xunit;

namespace Spx.DeltaWorker.Tests;

public sealed class DeltaEngineTests
{
    [Fact]
    public async Task RunOnceAsync_when_disabled_is_noop()
    {
        var logger = new ListLogger<DeltaEngine>();
        var options = Options.Create(new DeltaOptions { Enabled = false, PollIntervalSeconds = 5 });
        var engine = new DeltaEngine(logger, options);

        await engine.RunOnceAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Message.Contains("disabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunOnceAsync_when_enabled_logs_placeholder()
    {
        var logger = new ListLogger<DeltaEngine>();
        var options = Options.Create(new DeltaOptions { Enabled = true, PollIntervalSeconds = 5 });
        var engine = new DeltaEngine(logger, options);

        await engine.RunOnceAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Message.Contains("no-op", StringComparison.OrdinalIgnoreCase));
    }
}
