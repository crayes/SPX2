using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Configuration;
using Spx.DeltaWorker.Hosting;
using Xunit;

namespace Spx.DeltaWorker.Tests;

public sealed class DeltaOptionsTests
{
    [Fact]
    public void Options_binding_works_for_valid_values()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Delta:Enabled"] = "true",
                ["Delta:PollIntervalSeconds"] = "10"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDeltaWorker(config);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DeltaOptions>>().Value;

        Assert.True(options.Enabled);
        Assert.Equal(10, options.PollIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(10), options.PollInterval);
    }

    [Fact]
    public void Options_validation_fails_for_out_of_range_poll_interval()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Delta:Enabled"] = "true",
                ["Delta:PollIntervalSeconds"] = "0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDeltaWorker(config);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<DeltaOptions>>().Value);
    }
}
