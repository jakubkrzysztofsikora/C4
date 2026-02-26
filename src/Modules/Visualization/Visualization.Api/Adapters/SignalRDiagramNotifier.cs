using C4.Modules.Visualization.Api.Hubs;
using C4.Modules.Visualization.Application.Ports;
using Microsoft.AspNetCore.SignalR;

namespace C4.Modules.Visualization.Api.Adapters;

public sealed class SignalRDiagramNotifier(IHubContext<DiagramHub, IDiagramClient> hubContext) : IDiagramNotifier
{
    public async Task NotifyDiagramUpdatedAsync(Guid projectId, string diagramJson, CancellationToken cancellationToken) =>
        await hubContext.Clients.Group(projectId.ToString()).DiagramUpdated(projectId, diagramJson);

    public async Task NotifyHealthOverlayChangedAsync(Guid projectId, string healthJson, CancellationToken cancellationToken) =>
        await hubContext.Clients.Group(projectId.ToString()).HealthOverlayChanged(projectId, healthJson);
}
