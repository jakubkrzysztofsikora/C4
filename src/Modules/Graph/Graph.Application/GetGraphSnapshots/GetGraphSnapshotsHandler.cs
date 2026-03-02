using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraphSnapshots;

public sealed class GetGraphSnapshotsHandler(
    IArchitectureGraphRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetGraphSnapshotsQuery, Result<GetGraphSnapshotsResponse>>
{
    public async Task<Result<GetGraphSnapshotsResponse>> Handle(GetGraphSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetGraphSnapshotsResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetGraphSnapshotsResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var snapshots = graph.Snapshots
            .Where(s => !IsEmptySnapshot(s))
            .OrderBy(s => s.CreatedAtUtc)
            .Select(s => new GraphSnapshotDto(s.Id.Value, s.CreatedAtUtc, s.Source))
            .ToArray();

        return Result<GetGraphSnapshotsResponse>.Success(new GetGraphSnapshotsResponse(request.ProjectId, snapshots));
    }

    private static bool IsEmptySnapshot(Domain.GraphSnapshot.GraphSnapshot snapshot)
        => snapshot.Nodes.Count == 0 && snapshot.Edges.Count == 0;
}
