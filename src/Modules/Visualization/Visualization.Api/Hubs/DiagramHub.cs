using Microsoft.AspNetCore.SignalR;

namespace C4.Modules.Visualization.Api.Hubs;

public sealed class DiagramHub : Hub<IDiagramClient>
{
    public async Task JoinProject(Guid projectId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, projectId.ToString());

    public async Task LeaveProject(Guid projectId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId.ToString());
}
