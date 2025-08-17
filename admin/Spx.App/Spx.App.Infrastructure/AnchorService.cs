using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spx.App.Core;

namespace Spx.App.Infrastructure;

public sealed class AnchorService : IAnchorService
{
    private readonly IDbService _db;
    private readonly IQueueService _queue;
    private readonly IGraphService _graph;

    public AnchorService(IDbService db, IQueueService queue, IGraphService graph)
    { _db = db; _queue = queue; _graph = graph; }

    public async Task<string> RunAsync(CancellationToken ct)
    {
        var sb = new StringBuilder();
        async Task Step(string name, Task<(bool ok, string msg)> t)
        {
            var (ok, msg) = await t;
            sb.AppendLine($"{(ok ? "[OK]" : "[!!]")} {name}: {msg}");
        }

        await Step("Test DB", _db.TestAsync(ct));
        await Step("Ensure DB Schema", _db.EnsureSchemaAsync(ct));
        await Step("Test ASB", _queue.TestAsync(ct));
        await Step("Ensure Queue", _queue.EnsureQueueAsync(ct));
        await Step("ASB Send Test", _queue.SendTestAsync(ct));
        await Step("Test Graph", _graph.TestAsync(ct));

        sb.AppendLine("Smoke/Reset finalizado.");
        return sb.ToString();
    }
}
