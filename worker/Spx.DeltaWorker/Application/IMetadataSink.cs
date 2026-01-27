namespace Spx.DeltaWorker.Application;

public interface IMetadataSink
{
    Task WriteAsync(MetadataRecord record, CancellationToken cancellationToken);
}