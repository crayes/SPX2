using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Configuration;

namespace Spx.DeltaWorker.Infrastructure.Graph;

public sealed class GraphApiClient(HttpClient httpClient, TokenCredential credential, IOptions<SharePointOptions> options)
{
    private static readonly string[] Scopes = ["https://graph.microsoft.com/.default"];

    public async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(options.Value.HttpTimeoutSeconds);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var token = await credential.GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}