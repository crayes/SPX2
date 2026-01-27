using System.ComponentModel.DataAnnotations;

namespace Spx.DeltaWorker.Configuration;

public sealed class DeltaOptions
{
    public const string SectionName = "Delta";

    [Range(1, 3600)]
    public int PollIntervalSeconds { get; init; } = 5;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(PollIntervalSeconds);

    public bool Enabled { get; init; } = false;
}
