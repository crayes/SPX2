using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Application;
using Spx.DeltaWorker.Configuration;
using Spx.DeltaWorker.Infrastructure;
using Spx.DeltaWorker.Infrastructure.Graph;
using Spx.DeltaWorker.Infrastructure.Sinks;
using Spx.DeltaWorker.Infrastructure.State;

namespace Spx.DeltaWorker.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeltaWorker(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configurações
        services.AddOptions<DeltaOptions>()
            .Bind(configuration.GetSection(DeltaOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddOptions<SharePointOptions>()
            .Bind(configuration.GetSection(SharePointOptions.SectionName));

        services.AddSingleton<IValidateOptions<SharePointOptions>, SharePointOptionsValidator>();

        // 2. Credenciais Azure
        services.AddSingleton<TokenCredential>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SharePointOptions>>().Value;
            return new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        });

        // 3. Rate limiter adaptativo (portado do Python) - ANTES do GraphApiClient
        services.AddSingleton<AdaptiveRateLimiter>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DeltaOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<AdaptiveRateLimiter>>();
            return new AdaptiveRateLimiter(logger, options.RateLimitPerSecond);
        });

        // 4. HTTP Client para Graph API
        services.AddHttpClient<GraphApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SharePointOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
        });

        // 5. State e Sink
        services.AddSingleton<IDeltaStateStore, FileDeltaStateStore>();
        services.AddSingleton<IMetadataSink, NdjsonFileMetadataSink>();

        // 6. Updater para PATCH nos campos do SharePoint
        services.AddSingleton<SharePointFieldsUpdater>();

        // 7. Delta Engine
        services.AddSingleton<IDeltaEngine, SharePointDeltaEngine>();
        services.AddHostedService<Worker>();

        return services;
    }
}
