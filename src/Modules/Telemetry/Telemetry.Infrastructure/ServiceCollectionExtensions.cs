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
        var appId = configuration["ApplicationInsights:AppId"];
        if (!string.IsNullOrWhiteSpace(appId))
        {
            services.AddSingleton<IApplicationInsightsClient, ApplicationInsightsClient>();
        }

        return services;
    }
}
