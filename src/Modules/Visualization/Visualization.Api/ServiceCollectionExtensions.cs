using C4.Modules.Visualization.Api.Persistence;
using C4.Modules.Visualization.Application.Ports;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Visualization.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVisualizationModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Visualization.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddSingleton<IDiagramReadModel, InMemoryDiagramReadModel>();
        services.AddSingleton<IViewPresetRepository, InMemoryViewPresetRepository>();
        services.AddSingleton<IDiagramExporter, SvgDiagramExporter>();
        services.AddSingleton<IDiagramExporter, PdfDiagramExporter>();
        services.AddSingleton<IUnitOfWork, NoOpVisualizationUnitOfWork>();
        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
