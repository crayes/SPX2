namespace Spx.DeltaWorker.Infrastructure.State;

public interface IDeltaStateStore
{
    Task<DeltaState?> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(DeltaState state, CancellationToken cancellationToken);
}