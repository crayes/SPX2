using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spx.DeltaWorker.Application;
using Spx.DeltaWorker.Configuration;

namespace Spx.DeltaWorker.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeltaWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DeltaOptions>()
            .Bind(configuration.GetSection(DeltaOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<IDeltaEngine, DeltaEngine>();
        services.AddHostedService<Worker>();

        return services;
    }
}
