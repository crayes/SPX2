using System.Threading;
using System.Threading.Tasks;

namespace Spx.App.Core;

public interface IDbService
{
    Task<(bool ok, string msg)> TestAsync(CancellationToken ct);
    Task<(bool ok, string msg)> EnsureSchemaAsync(CancellationToken ct);
}

public interface IQueueService
{
    Task<(bool ok, string msg)> TestAsync(CancellationToken ct);
    Task<(bool ok, string msg)> EnsureQueueAsync(CancellationToken ct);
    Task<(bool ok, string msg)> SendTestAsync(CancellationToken ct);
}

public interface IGraphService
{
    Task<(bool ok, string msg)> TestAsync(CancellationToken ct);
}

public interface IAnchorService
{
    Task<string> RunAsync(CancellationToken ct);
}
