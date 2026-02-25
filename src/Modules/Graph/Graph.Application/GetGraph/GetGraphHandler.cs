using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraph;

public sealed class GetGraphHandler(IArchitectureGraphRepository repository) : IRequestHandler<GetGraphQuery, Result<GraphDto>>
{
    public async Task<Result<GraphDto>> Handle(GetGraphQuery request, CancellationToken cancellationToken)
    {
        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GraphDto>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var nodes = graph.Nodes.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Level) && Enum.TryParse<C4Level>(request.Level, true, out var level))
        {
            nodes = nodes.Where(n => n.Level == level);
        }

        var nodeList = nodes.ToArray();
        var nodeIds = nodeList.Select(n => n.Id).ToHashSet();
        var edges = graph.Edges.Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId));

        return Result<GraphDto>.Success(new GraphDto(
            request.ProjectId,
            nodeList.Select(n => new GraphNodeDto(n.Id.Value, n.Name, n.ExternalResourceId, n.Level.ToString())).ToArray(),
            edges.Select(e => new GraphEdgeDto(e.Id.Value, e.SourceNodeId.Value, e.TargetNodeId.Value)).ToArray()));
    }
}
