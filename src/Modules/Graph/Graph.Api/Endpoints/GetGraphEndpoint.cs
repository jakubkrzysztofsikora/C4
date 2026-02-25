using C4.Modules.Graph.Application.GetGraph;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class GetGraphEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/graph", async (Guid projectId, string? level, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGraphQuery(projectId, level), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .RequireAuthorization();
    }
}
