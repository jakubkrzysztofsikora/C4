using C4.Modules.Discovery.Api.Adapters;
using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Adapters;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Infrastructure;
using C4.Modules.Discovery.Infrastructure.AI;
using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Modules.Discovery.Infrastructure.Persistence.Repositories;
using C4.Modules.Discovery.Infrastructure.Security;
using C4.Modules.Discovery.Infrastructure.Services;
using C4.Shared.Infrastructure.AI;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        var connectionString = configuration.GetConnectionString("Discovery");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<DiscoveryDbContext>(options => options.UseInMemoryDatabase("discovery-dev"));
            services.AddSingleton<IAzureSubscriptionRepository, InMemoryAzureSubscriptionRepository>();
            services.AddSingleton<IDiscoveredResourceRepository, InMemoryDiscoveredResourceRepository>();
            services.AddSingleton<IDriftResultRepository, InMemoryDriftResultRepository>();
            services.AddSingleton<IDriftQueryService, InMemoryDriftQueryService>();
            services.AddSingleton<IMcpServerConfigRepository, InMemoryMcpServerConfigRepository>();
            services.AddSingleton<IArchitectureContextRepository, InMemoryArchitectureContextRepository>();
            services.AddSingleton<NoOpDiscoveryUnitOfWork>();
            services.AddKeyedSingleton<IUnitOfWork>("Discovery", (sp, _) => sp.GetRequiredService<NoOpDiscoveryUnitOfWork>());
        }
        else
        {
            services.AddDbContext<DiscoveryDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IAzureSubscriptionRepository, AzureSubscriptionRepository>();
            services.AddScoped<IDiscoveredResourceRepository, DiscoveredResourceRepository>();
            services.AddScoped<IDriftResultRepository, DriftResultRepository>();
            services.AddScoped<IDriftQueryService, DriftQueryService>();
            services.AddScoped<IMcpServerConfigRepository, McpServerConfigRepository>();
            services.AddScoped<IArchitectureContextRepository, ArchitectureContextRepository>();
            services.AddKeyedScoped<IUnitOfWork>("Discovery", (sp, _) => sp.GetRequiredService<DiscoveryDbContext>());
        }

        services.AddSharedSemanticKernel(configuration);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IAzureTokenStore, InMemoryAzureTokenStore>();
        }
        else
        {
            services.AddSingleton<IAzureTokenStore, DatabaseAzureTokenStore>();
        }
        services.AddScoped<IApplicationInsightsTokenProvider, DiscoveryApplicationInsightsTokenProvider>();
        services.AddHttpClient();
        services.AddSingleton<IAzureIdentityService, AzureIdentityService>();
        services.AddSingleton<IOAuthStateStore, InMemoryOAuthStateStore>();

        var azureClientId = configuration["AzureAd:ClientId"];
        if (!string.IsNullOrWhiteSpace(azureClientId))
        {
            services.AddSingleton<IAzureResourceGraphClient, AzureResourceGraphClient>();
        }
        else
        {
            services.AddSingleton<IAzureResourceGraphClient, FakeAzureResourceGraphClient>();
        }
        services.AddSingleton<IDiscoverySourceAdapter, AzureSubscriptionDiscoverySourceAdapter>();
        services.AddSingleton<IDiscoverySourceAdapter, RepositoryIacDiscoverySourceAdapter>();
        services.AddSingleton<IDiscoverySourceAdapter, RemoteMcpDiscoverySourceAdapter>();
        services.AddSingleton<IDiscoveryInputProvider, CompositeDiscoveryInputProvider>();

        services.AddSingleton<BicepParser>();
        services.AddSingleton<TerraformParser>();
        services.AddSingleton<IIacStateParser, CompositeIacStateParser>();
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
        services.AddScoped<IResourceClassifier>(sp =>
        {
            var result = sp.GetRequiredKeyedService<SemanticKernelCreationResult>("Discovery");
            return result.EnabledTools.Contains(nameof(ResourceClassifierPlugin))
                ? new ResourceClassifierPlugin(
                    result.Kernel,
                    sp.GetService<C4.Shared.Kernel.Contracts.ILearningProvider>(),
                    sp.GetService<C4.Shared.Kernel.Contracts.IProjectArchitectureContextProvider>(),
                    sp.GetService<ILogger<ResourceClassifierPlugin>>())
                : throw new InvalidOperationException(
                    $"{nameof(ResourceClassifierPlugin)} is required but disabled by the tool filter. " +
                    $"Update the SemanticKernel:ToolFiltersByEnvironment configuration to enable it.");
        });
        services.AddSingleton<IDiscoveryInputPlanner, DiscoveryInputPlanner>();
        services.AddSingleton<IArchitectureQuestionGenerator, LlmArchitectureQuestionGenerator>();
        services.AddScoped<C4.Shared.Kernel.Contracts.IProjectArchitectureContextProvider, ProjectArchitectureContextProvider>();

        services.AddEndpoints(AssemblyReference.Assembly);

        return services;
    }
}
