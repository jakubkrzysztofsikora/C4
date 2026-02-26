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
        services.AddSingleton(sp =>
            sp.GetRequiredService<ISemanticKernelFactory>().Create("Graph",
                [nameof(ArchitectureAnalysisPlugin), nameof(ThreatDetectionPlugin)]));
        services.AddSingleton<IArchitectureAnalyzer, ArchitectureAnalysisPlugin>();
        services.AddSingleton<IThreatDetector, ThreatDetectionPlugin>();

        services.AddScoped<IArchitectureGraphRepository, ArchitectureGraphRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GraphDbContext>());
        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
