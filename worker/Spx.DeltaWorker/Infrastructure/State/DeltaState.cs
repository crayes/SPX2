namespace Spx.DeltaWorker.Infrastructure.State;

public sealed record DeltaState(
    string? ContinuationLink,
    string? DeltaLink,
    string? SiteId,
    string? DriveId,
    string? DriveName,
    DateTimeOffset SavedAtUtc);