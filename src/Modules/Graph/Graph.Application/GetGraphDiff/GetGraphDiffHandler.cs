using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraphDiff;

public sealed class GetGraphDiffHandler(
    IArchitectureGraphRepository repository,
    IProjectAuthorizationService authorizationService
) : IRequestHandler<GetGraphDiffQuery, Result<GetGraphDiffResponse>>
{
    public async Task<Result<GetGraphDiffResponse>> Handle(GetGraphDiffQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetGraphDiffResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetGraphDiffResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var from = graph.Snapshots.FirstOrDefault(s => s.Id.Value == request.FromSnapshotId);
        var to = graph.Snapshots.FirstOrDefault(s => s.Id.Value == request.ToSnapshotId);
        if (from is null || to is null) return Result<GetGraphDiffResponse>.Success(new([], [], [], []));

        var fromSet = from.Nodes.Select(n => n.ExternalResourceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toSet = to.Nodes.Select(n => n.ExternalResourceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = toSet.Except(fromSet).ToArray();
        var removed = fromSet.Except(toSet).ToArray();

        var fromEdges = from.Edges.Select(e => $"{e.SourceNodeId}->{e.TargetNodeId}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toEdges = to.Edges.Select(e => $"{e.SourceNodeId}->{e.TargetNodeId}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addedEdges = toEdges.Except(fromEdges).ToArray();
        var removedEdges = fromEdges.Except(toEdges).ToArray();

        return Result<GetGraphDiffResponse>.Success(new GetGraphDiffResponse(added, removed, addedEdges, removedEdges));
    }
}
