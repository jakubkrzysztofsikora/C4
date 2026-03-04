using C4.Modules.Graph.Application.GetGraphQuality;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class GetGraphQualityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/analysis/graph-quality", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGraphQualityQuery(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization();
    }
}
