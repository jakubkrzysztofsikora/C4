using System.Text.Json;
using C4.Modules.Visualization.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;

namespace C4.Modules.Visualization.Application.IntegrationEventHandlers;

internal sealed class TelemetryUpdatedHandler(IDiagramNotifier notifier) : INotificationHandler<TelemetryUpdatedIntegrationEvent>
{
    public async Task Handle(TelemetryUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        string healthJson = JsonSerializer.Serialize(notification.Services);
        await notifier.NotifyHealthOverlayChangedAsync(notification.ProjectId, healthJson, cancellationToken);
    }
}
