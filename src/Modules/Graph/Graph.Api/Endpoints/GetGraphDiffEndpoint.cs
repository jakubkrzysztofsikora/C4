using C4.Modules.Graph.Application.GetGraphDiff;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class GetGraphDiffEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/graph/diff", async (Guid projectId, Guid fromSnapshotId, Guid toSnapshotId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGraphDiffQuery(projectId, fromSnapshotId, toSnapshotId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
    }
}
