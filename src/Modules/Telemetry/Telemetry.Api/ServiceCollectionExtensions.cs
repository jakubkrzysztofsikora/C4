using C4.Modules.Telemetry.Api.Adapters;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Infrastructure;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using C4.Modules.Telemetry.Infrastructure.Repositories;
using C4.Modules.Telemetry.Infrastructure.Services;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Telemetry.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelemetryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Telemetry.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        var connectionString = configuration.GetConnectionString("Telemetry");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<TelemetryDbContext>(options => options.UseInMemoryDatabase("telemetry-dev"));
        }
        else
        {
            services.AddDbContext<TelemetryDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<IAppInsightsConfigStore, AppInsightsConfigStore>();
        services.AddTelemetryTargetStore();
        services.AddKeyedScoped<IUnitOfWork>("Telemetry", (sp, _) => sp.GetRequiredService<TelemetryDbContext>());
        services.AddScoped<ITelemetryQueryService, TelemetryQueryService>();
        services.AddHttpClient();
        services.AddApplicationInsightsClient(configuration);

        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
