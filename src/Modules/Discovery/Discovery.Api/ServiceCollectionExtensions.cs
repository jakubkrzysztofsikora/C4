using C4.Modules.Discovery.Api.Adapters;
using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Adapters;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Infrastructure.AI;
using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Modules.Discovery.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.AI;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscoveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Discovery.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(C4.Modules.Discovery.Application.AssemblyReference.Assembly);

        services.AddSharedSemanticKernel(configuration);

        services.AddSingleton<IAzureSubscriptionRepository, InMemoryAzureSubscriptionRepository>();
        services.AddSingleton<IDiscoveredResourceRepository, InMemoryDiscoveredResourceRepository>();
        services.AddSingleton<IDriftResultRepository, InMemoryDriftResultRepository>();
        services.AddSingleton<IAzureResourceGraphClient, FakeAzureResourceGraphClient>();
        services.AddSingleton<IDiscoverySourceAdapter, AzureSubscriptionDiscoverySourceAdapter>();
        services.AddSingleton<IDiscoverySourceAdapter, RepositoryIacDiscoverySourceAdapter>();
        services.AddSingleton<IDiscoverySourceAdapter, RemoteMcpDiscoverySourceAdapter>();
        services.AddSingleton<IDiscoveryInputProvider, CompositeDiscoveryInputProvider>();

        services.AddSingleton<BicepParser>();
        services.AddSingleton<TerraformParser>();
        services.AddSingleton<IIacStateParser, CompositeIacStateParser>();

        services.AddSingleton<IUnitOfWork, NoOpDiscoveryUnitOfWork>();
        services.AddSingleton<IDiscoveryDataPreparer, DiscoveryDataPreparer>();
        services.AddSingleton<MultiSourceDiscoveryPlanner>();
        services.AddSingleton<IDiscoveryTelemetryEventSink, DiscoveryStructuredTelemetryEventSink>();
        services.AddSingleton<DiscoveryPromptRenderFilter>();
        services.AddSingleton<DiscoveryFunctionInvocationFilter>();
        services.AddKeyedSingleton<SemanticKernelCreationResult>("Discovery", (sp, _) =>
            sp.GetRequiredService<ISemanticKernelFactory>().Create("Discovery",
                [nameof(ResourceClassifierPlugin)]));
        services.AddSingleton(sp =>
        {
            var result = sp.GetRequiredKeyedService<SemanticKernelCreationResult>("Discovery");
            result.Kernel.PromptRenderFilters.Add(sp.GetRequiredService<DiscoveryPromptRenderFilter>());
            result.Kernel.FunctionInvocationFilters.Add(sp.GetRequiredService<DiscoveryFunctionInvocationFilter>());
            return result.Kernel;
        });
        services.AddSingleton<IResourceClassifier>(sp =>
        {
            var result = sp.GetRequiredKeyedService<SemanticKernelCreationResult>("Discovery");
            return result.EnabledTools.Contains(nameof(ResourceClassifierPlugin))
                ? new ResourceClassifierPlugin(result.Kernel)
                : throw new InvalidOperationException(
                    $"{nameof(ResourceClassifierPlugin)} is required but disabled by the tool filter. " +
                    $"Update the SemanticKernel:ToolFiltersByEnvironment configuration to enable it.");
        });
        services.AddSingleton<IDiscoveryInputPlanner, DiscoveryInputPlanner>();

        services.AddEndpoints(AssemblyReference.Assembly);

        return services;
    }
}
