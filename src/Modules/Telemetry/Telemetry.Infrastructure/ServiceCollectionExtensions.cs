using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Infrastructure.Adapters;
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
}
