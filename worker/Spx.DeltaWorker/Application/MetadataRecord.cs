namespace Spx.DeltaWorker.Application;

public sealed record MetadataRecord(
    string ItemId,
    string Name,
    string? WebUrl,
    string? ParentPath,
    DateTimeOffset? LastModifiedUtc,
    IReadOnlyDictionary<string, object?> Fields);