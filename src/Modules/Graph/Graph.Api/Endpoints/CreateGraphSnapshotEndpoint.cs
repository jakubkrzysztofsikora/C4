using C4.Modules.Graph.Application.CreateGraphSnapshot;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class CreateGraphSnapshotEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/graph/snapshots", async (
            Guid projectId,
            CreateGraphSnapshotRequest? request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateGraphSnapshotCommand(projectId, request?.Source), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Error);
        }).RequireAuthorization();
    }

    public sealed record CreateGraphSnapshotRequest(string? Source);
}
