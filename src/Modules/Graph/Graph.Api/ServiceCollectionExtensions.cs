using C4.Modules.Graph.Api.Persistence;
using C4.Modules.Graph.Application.Ports;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Graph.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraphModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Graph.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddSingleton<IArchitectureGraphRepository, InMemoryArchitectureGraphRepository>();
        services.AddSingleton<IUnitOfWork, NoOpGraphUnitOfWork>();
        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
