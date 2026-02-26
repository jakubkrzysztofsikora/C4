using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Infrastructure.AI;
using C4.Modules.Graph.Infrastructure.Persistence;
using C4.Modules.Graph.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.AI;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Graph.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraphModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Graph.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddSharedSemanticKernel(configuration);

        var connectionString = configuration.GetConnectionString("Graph");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<GraphDbContext>(options => options.UseInMemoryDatabase("graph-dev"));
        }
        else
        {
            services.AddDbContext<GraphDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddKeyedSingleton<SemanticKernelCreationResult>("Graph", (sp, _) =>
            sp.GetRequiredService<ISemanticKernelFactory>().Create("Graph",
                [nameof(ArchitectureAnalysisPlugin), nameof(ThreatDetectionPlugin)]));
        services.AddSingleton(sp =>
            sp.GetRequiredKeyedService<SemanticKernelCreationResult>("Graph").Kernel);
        services.AddSingleton<IArchitectureAnalyzer>(sp =>
        {
            var result = sp.GetRequiredKeyedService<SemanticKernelCreationResult>("Graph");
            return result.EnabledTools.Contains(nameof(ArchitectureAnalysisPlugin))
                ? new ArchitectureAnalysisPlugin(result.Kernel, sp.GetService<C4.Shared.Kernel.Contracts.ILearningProvider>())
                : throw new InvalidOperationException(
                    $"{nameof(ArchitectureAnalysisPlugin)} is required but disabled by the tool filter. " +
                    $"Update the SemanticKernel:ToolFiltersByEnvironment configuration to enable it.");
        });
        services.AddSingleton<IThreatDetector>(sp =>
        {
            var result = sp.GetRequiredKeyedService<SemanticKernelCreationResult>("Graph");
            return result.EnabledTools.Contains(nameof(ThreatDetectionPlugin))
                ? new ThreatDetectionPlugin(result.Kernel, sp.GetService<C4.Shared.Kernel.Contracts.ILearningProvider>())
                : throw new InvalidOperationException(
                    $"{nameof(ThreatDetectionPlugin)} is required but disabled by the tool filter. " +
                    $"Update the SemanticKernel:ToolFiltersByEnvironment configuration to enable it.");
        });

        services.AddScoped<IArchitectureGraphRepository, ArchitectureGraphRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GraphDbContext>());
        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
