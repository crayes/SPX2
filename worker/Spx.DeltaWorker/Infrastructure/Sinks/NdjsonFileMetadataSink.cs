using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Application;
using Spx.DeltaWorker.Configuration;

namespace Spx.DeltaWorker.Infrastructure.Sinks;

public sealed class NdjsonFileMetadataSink(IHostEnvironment hostEnvironment, IOptions<SharePointOptions> options)
    : IMetadataSink
{
    private readonly SharePointOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task WriteAsync(MetadataRecord record, CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, _options.OutputNdjsonPath));
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? hostEnvironment.ContentRootPath);

        var json = JsonSerializer.Serialize(record);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(path, json + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}