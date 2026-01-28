using System.ComponentModel.DataAnnotations;

namespace Spx.DeltaWorker.Configuration;

public sealed class DeltaOptions
{
    public const string SectionName = "Delta";

    [Range(1, 3600)]
    public int PollIntervalSeconds { get; init; } = 5;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(PollIntervalSeconds);

    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Número máximo de workers paralelos para processamento.
    /// Baseado no Python: MAX_WORKERS = 20
    /// </summary>
    [Range(1, 50)]
    public int MaxWorkers { get; init; } = 20;

    /// <summary>
    /// Rate limit máximo de requests por segundo.
    /// Baseado no Python: RATE_LIMIT_REQUESTS = 20
    /// </summary>
    [Range(1, 100)]
    public int RateLimitPerSecond { get; init; } = 20;
}
