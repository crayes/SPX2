using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Spx.DeltaWorker.Application;
using Spx.DeltaWorker.Configuration;
using Spx.DeltaWorker.Infrastructure.Graph;
using Spx.DeltaWorker.Infrastructure.Sinks;
using Spx.DeltaWorker.Infrastructure.State;

namespace Spx.DeltaWorker.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeltaWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DeltaOptions>()
            .Bind(configuration.GetSection(DeltaOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddOptions<SharePointOptions>()
            .Bind(configuration.GetSection(SharePointOptions.SectionName));

        services.AddSingleton<IValidateOptions<SharePointOptions>, SharePointOptionsValidator>();

        services.AddSingleton<TokenCredential>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SharePointOptions>>().Value;
            return new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        });

        services.AddHttpClient<GraphApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SharePointOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
        });

        services.AddSingleton<IDeltaStateStore, FileDeltaStateStore>();
        services.AddSingleton<IMetadataSink, NdjsonFileMetadataSink>();

        // Updater para PATCH nos campos do SharePoint
        services.AddSingleton<SharePointFieldsUpdater>();

        services.AddSingleton<IDeltaEngine, SharePointDeltaEngine>();
        services.AddHostedService<Worker>();

        return services;
    }
}
