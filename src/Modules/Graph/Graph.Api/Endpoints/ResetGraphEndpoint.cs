using C4.Modules.Graph.Application.ResetGraph;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class ResetGraphEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/projects/{projectId:guid}/graph", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResetGraphCommand(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
