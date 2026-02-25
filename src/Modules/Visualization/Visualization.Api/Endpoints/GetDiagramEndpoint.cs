using C4.Modules.Visualization.Application.GetDiagram;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Visualization.Api.Endpoints;

public sealed class GetDiagramEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/diagram", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDiagramQuery(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .RequireAuthorization();
    }
}
