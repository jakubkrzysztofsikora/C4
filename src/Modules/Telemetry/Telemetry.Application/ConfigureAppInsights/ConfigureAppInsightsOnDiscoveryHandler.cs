using C4.Modules.Telemetry.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Telemetry.Application.ConfigureAppInsights;

public sealed class ConfigureAppInsightsOnDiscoveryHandler(
    IAppInsightsConfigStore configStore,
    ILogger<ConfigureAppInsightsOnDiscoveryHandler> logger)
    : INotificationHandler<AppInsightsDiscoveredEvent>
{
    public async Task Handle(AppInsightsDiscoveredEvent notification, CancellationToken cancellationToken)
    {
        await configStore.StoreAsync(
            notification.ProjectId,
            notification.AppId,
            notification.InstrumentationKey,
            cancellationToken);

        logger.LogInformation(
            "Stored Application Insights target {AppId} for project {ProjectId}. Telemetry sync is deferred to explicit/manual sync endpoints.",
            notification.AppId,
            notification.ProjectId);
    }
}
