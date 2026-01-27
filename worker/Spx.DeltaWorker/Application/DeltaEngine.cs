using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Configuration;

namespace Spx.DeltaWorker.Application;

public sealed class DeltaEngine(ILogger<DeltaEngine> logger, IOptions<DeltaOptions> options) : IDeltaEngine
{
    private readonly DeltaOptions _options = options.Value;

    public Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            logger.LogDebug("Delta processing is disabled (Delta:Enabled=false). Skipping tick.");
            return Task.CompletedTask;
        }

        // Placeholder: business logic was not recoverable from the repository state.
        // Keep this as a no-op until the real logic is restored/migrated.
        logger.LogInformation("DeltaEngine tick executed (no-op placeholder).");
        return Task.CompletedTask;
    }
}
