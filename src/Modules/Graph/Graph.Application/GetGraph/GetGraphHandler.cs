using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraph;

public sealed class GetGraphHandler(
    IArchitectureGraphRepository repository,
    ITelemetryQueryService telemetryQueryService,
    IDriftQueryService driftQueryService,
    IProjectAuthorizationService authorizationService
) : IRequestHandler<GetGraphQuery, Result<GraphDto>>
{
    public async Task<Result<GraphDto>> Handle(GetGraphQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GraphDto>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GraphDto>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var nodes = graph.Nodes.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Level) && Enum.TryParse<C4Level>(request.Level, true, out var level))
        {
            nodes = nodes.Where(n => n.Level == level);
        }

        var nodeList = nodes.ToArray();
        var nodeIds = nodeList.Select(n => n.Id).ToHashSet();
        var edges = graph.Edges.Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId)).ToArray();

        var resourceIds = nodeList.Select(n => n.ExternalResourceId).ToArray();

        var healthTask = telemetryQueryService.GetServiceHealthSummariesAsync(request.ProjectId, cancellationToken);
        var driftTask = driftQueryService.GetDriftedResourceIdsAsync(resourceIds, cancellationToken);
        await Task.WhenAll(healthTask, driftTask);

        var healthByService = healthTask.Result.ToDictionary(s => s.Service, s => s, StringComparer.OrdinalIgnoreCase);
        var driftedSet = new HashSet<string>(driftTask.Result, StringComparer.OrdinalIgnoreCase);

        var nodeDtos = nodeList.Select(n =>
        {
            var isDrifted = driftedSet.Contains(n.ExternalResourceId);
            var environment = EnvironmentClassifier.InferEnvironment(n.Name);
            var serviceType = n.Properties.Technology is "unknown" or "" ? "external" : n.Properties.Technology;
            var resourceGroup = ExtractResourceGroup(n.ExternalResourceId) ?? "";
            if (healthByService.TryGetValue(n.Name, out var summary))
            {
                return new GraphNodeDto(n.Id.Value, n.Name, n.ExternalResourceId, n.Level.ToString(), summary.Status.ToLower(), summary.Score, n.ParentId?.Value, isDrifted, environment, serviceType, resourceGroup);
            }
            return new GraphNodeDto(n.Id.Value, n.Name, n.ExternalResourceId, n.Level.ToString(), "green", 1.0, n.ParentId?.Value, isDrifted, environment, serviceType, resourceGroup);
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

    private static string? ExtractResourceGroup(string resourceId)
    {
        var lower = resourceId.ToLowerInvariant();
        var rgIndex = lower.IndexOf("/resourcegroups/", StringComparison.Ordinal);
        if (rgIndex < 0) return null;

        var start = rgIndex + "/resourcegroups/".Length;
        var end = lower.IndexOf('/', start);
        if (end < 0) return lower[start..];

        return lower[start..end];
    }
}
