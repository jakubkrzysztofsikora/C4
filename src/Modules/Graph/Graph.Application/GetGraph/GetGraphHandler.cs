using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraph;

public sealed class GetGraphHandler(
    IArchitectureGraphRepository repository,
    ITelemetryQueryService telemetryQueryService
) : IRequestHandler<GetGraphQuery, Result<GraphDto>>
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
        var edges = graph.Edges.Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId)).ToArray();

        var healthSummaries = await telemetryQueryService.GetServiceHealthSummariesAsync(request.ProjectId, cancellationToken);
        var healthByService = healthSummaries.ToDictionary(s => s.Service, s => s, StringComparer.OrdinalIgnoreCase);

        var nodeDtos = nodeList.Select(n =>
        {
            if (healthByService.TryGetValue(n.Name, out var summary))
            {
                return new GraphNodeDto(n.Id.Value, n.Name, n.ExternalResourceId, n.Level.ToString(), summary.Status.ToLower(), summary.Score, n.ParentId?.Value);
            }
            return new GraphNodeDto(n.Id.Value, n.Name, n.ExternalResourceId, n.Level.ToString(), "green", 1.0, n.ParentId?.Value);
        }).ToArray();

        var healthScoreById = nodeDtos.ToDictionary(n => n.Id, n => n.HealthScore);

        var edgeDtos = edges.Select(e =>
        {
            var sourceScore = healthScoreById.GetValueOrDefault(e.SourceNodeId.Value, 1.0);
            var targetScore = healthScoreById.GetValueOrDefault(e.TargetNodeId.Value, 1.0);
            var traffic = (sourceScore + targetScore) / 2.0;
            return new GraphEdgeDto(e.Id.Value, e.SourceNodeId.Value, e.TargetNodeId.Value, traffic);
        }).ToArray();

        return Result<GraphDto>.Success(new GraphDto(request.ProjectId, nodeDtos, edgeDtos));
    }
}
