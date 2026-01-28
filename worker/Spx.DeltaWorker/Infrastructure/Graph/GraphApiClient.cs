using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Configuration;

namespace Spx.DeltaWorker.Infrastructure.Graph;

public sealed class GraphApiClient(
    HttpClient httpClient,
    TokenCredential credential,
    IOptions<SharePointOptions> options,
    ILogger<GraphApiClient> logger)
{
    private static readonly string[] Scopes = ["https://graph.microsoft.com/.default"];
    private const int MaxRetries = 3;

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

    /// <summary>
    /// Envia PATCH request com retry e rate limiting handling.
    /// </summary>
    public async Task<bool> PatchJsonAsync(string url, object payload, CancellationToken cancellationToken)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(options.Value.HttpTimeoutSeconds);

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var token = await credential.GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // Handle rate limiting (HTTP 429)
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);
                    logger.LogWarning("Rate limited (429). Waiting {seconds}s before retry.", retryAfter.TotalSeconds);
                    await Task.Delay(retryAfter, cancellationToken);
                    continue;
                }

                // Handle service unavailable (HTTP 503)
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 5);
                    logger.LogWarning("Service unavailable (503). Waiting {seconds}s before retry.", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                // Handle not found (HTTP 404) - item might have been deleted
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.LogDebug("Item not found (404) at {url}", url);
                    return false;
                }

                // Log other errors
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("PATCH failed with {statusCode}: {error}", response.StatusCode, errorContent);

                // Retry for server errors
                if ((int)response.StatusCode >= 500)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return false;
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries - 1)
            {
                logger.LogWarning(ex, "HTTP request failed on attempt {attempt}. Retrying...", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex) when (attempt < MaxRetries - 1)
            {
                logger.LogWarning(ex, "Request timeout on attempt {attempt}. Retrying...", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }

        return false;
    }
}
