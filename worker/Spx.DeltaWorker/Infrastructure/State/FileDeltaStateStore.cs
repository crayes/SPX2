using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Configuration;

namespace Spx.DeltaWorker.Infrastructure.State;

public sealed class FileDeltaStateStore(IHostEnvironment hostEnvironment, IOptions<SharePointOptions> options)
    : IDeltaStateStore
{
    private readonly SharePointOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<DeltaState?> LoadAsync(CancellationToken cancellationToken)
    {
        var path = GetAbsolutePath();
        if (!File.Exists(path))
        {
            return null;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<DeltaState>(stream, cancellationToken: cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(DeltaState state, CancellationToken cancellationToken)
    {
        var path = GetAbsolutePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? hostEnvironment.ContentRootPath);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, state, cancellationToken: cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetAbsolutePath()
        => Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, _options.DeltaStateFile));
}