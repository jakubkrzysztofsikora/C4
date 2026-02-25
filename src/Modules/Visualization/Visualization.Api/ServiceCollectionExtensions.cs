using C4.Modules.Visualization.Api.Adapters;
using C4.Modules.Visualization.Api.Hubs;
using C4.Modules.Visualization.Api.Persistence;
using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Infrastructure.Persistence;
using C4.Modules.Visualization.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Visualization.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVisualizationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Visualization.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        var connectionString = configuration.GetConnectionString("Visualization");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<VisualizationDbContext>(options => options.UseInMemoryDatabase("visualization-dev"));
        }
        else
        {
            services.AddDbContext<VisualizationDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddSingleton<IDiagramReadModel, InMemoryDiagramReadModel>();
        services.AddScoped<IViewPresetRepository, ViewPresetRepository>();
        services.AddSingleton<IDiagramExporter, SvgDiagramExporter>();
        services.AddSingleton<IDiagramExporter, PdfDiagramExporter>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<VisualizationDbContext>());
        services.AddScoped<IDiagramNotifier, SignalRDiagramNotifier>();
        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
