using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Infrastructure.Adapters;
using C4.Modules.Telemetry.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Telemetry.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationInsightsClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IApplicationInsightsClient, ApplicationInsightsClient>();
        return services;
    }

    public static IServiceCollection AddTelemetryTargetStore(this IServiceCollection services)
    {
        services.AddScoped<ITelemetryTargetStore, TelemetryTargetStore>();
        return services;
    }
}
