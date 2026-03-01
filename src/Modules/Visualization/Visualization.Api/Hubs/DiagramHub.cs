using C4.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;

namespace C4.Modules.Visualization.Api.Hubs;

public sealed class DiagramHub(IProjectAuthorizationService authorizationService) : Hub<IDiagramClient>
{
    public async Task JoinProject(Guid projectId)
    {
        var authCheck = await authorizationService.AuthorizeAsync(projectId, Context.ConnectionAborted);
        if (authCheck.IsFailure)
            throw new HubException("Not authorized to access this project.");

        await Groups.AddToGroupAsync(Context.ConnectionId, projectId.ToString());
    }

    public async Task LeaveProject(Guid projectId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId.ToString());
}
