namespace Spx.DeltaWorker.Application;

public interface IDeltaEngine
{
    Task RunOnceAsync(CancellationToken cancellationToken);
}
