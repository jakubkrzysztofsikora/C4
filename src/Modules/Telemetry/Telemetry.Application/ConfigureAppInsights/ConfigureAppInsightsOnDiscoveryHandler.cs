using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Application.SyncApplicationInsightsTelemetry;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Telemetry.Application.ConfigureAppInsights;

public sealed class ConfigureAppInsightsOnDiscoveryHandler(
    IAppInsightsConfigStore configStore,
    IMediator mediator,
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

        try
        {
            await mediator.Send(
                new SyncApplicationInsightsTelemetryCommand(notification.ProjectId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Auto-sync of App Insights telemetry failed for project {ProjectId}; manual sync may be needed", notification.ProjectId);
        }
    }
}
