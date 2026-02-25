using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraphDiff;

public sealed class GetGraphDiffHandler(IArchitectureGraphRepository repository)
    : IRequestHandler<GetGraphDiffQuery, Result<GetGraphDiffResponse>>
{
    public async Task<Result<GetGraphDiffResponse>> Handle(GetGraphDiffQuery request, CancellationToken cancellationToken)
    {
        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetGraphDiffResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var from = graph.Snapshots.FirstOrDefault(s => s.Id.Value == request.FromSnapshotId);
        var to = graph.Snapshots.FirstOrDefault(s => s.Id.Value == request.ToSnapshotId);
        if (from is null || to is null) return Result<GetGraphDiffResponse>.Success(new([], []));

        var fromSet = from.Nodes.Select(n => n.ExternalResourceId).ToHashSet();
        var toSet = to.Nodes.Select(n => n.ExternalResourceId).ToHashSet();
        var added = toSet.Except(fromSet).ToArray();
        var removed = fromSet.Except(toSet).ToArray();

        return Result<GetGraphDiffResponse>.Success(new GetGraphDiffResponse(added, removed));
    }
}
