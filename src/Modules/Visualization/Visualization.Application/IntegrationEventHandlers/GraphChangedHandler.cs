using System.Text.Json;
using C4.Modules.Visualization.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;

namespace C4.Modules.Visualization.Application.IntegrationEventHandlers;

public sealed class GraphChangedHandler(IDiagramNotifier notifier) : INotificationHandler<GraphChangedIntegrationEvent>
{
    public async Task Handle(GraphChangedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            trigger = notification.Trigger,
            changedAtUtc = notification.ChangedAtUtc
        });

        await notifier.NotifyDiagramUpdatedAsync(notification.ProjectId, payload, cancellationToken);
    }
}
