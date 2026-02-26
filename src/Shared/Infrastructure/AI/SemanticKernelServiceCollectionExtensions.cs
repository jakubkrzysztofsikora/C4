using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace C4.Shared.Infrastructure.AI;

public static class SemanticKernelServiceCollectionExtensions
{
    public static IServiceCollection AddSharedSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SemanticKernelOptions>()
            .Bind(configuration.GetSection(SemanticKernelOptions.SectionName));

        services.TryAddSingleton<ISemanticKernelFactory, SemanticKernelFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISemanticKernelTelemetryHook, LoggingSemanticKernelTelemetryHook>());

        return services;
    }
}
