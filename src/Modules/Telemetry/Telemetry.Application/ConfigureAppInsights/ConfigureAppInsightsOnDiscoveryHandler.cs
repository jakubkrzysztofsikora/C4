using C4.Modules.Telemetry.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;

namespace C4.Modules.Telemetry.Application.ConfigureAppInsights;

public sealed class ConfigureAppInsightsOnDiscoveryHandler(IAppInsightsConfigStore configStore)
    : INotificationHandler<AppInsightsDiscoveredEvent>
{
    public async Task Handle(AppInsightsDiscoveredEvent notification, CancellationToken cancellationToken)
    {
        await configStore.StoreAsync(
            notification.ProjectId,
            notification.AppId,
            notification.InstrumentationKey,
            cancellationToken);
    }
}
