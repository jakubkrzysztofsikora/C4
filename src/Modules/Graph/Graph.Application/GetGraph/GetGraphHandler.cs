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

        C4Level? requestedLevel = ParseLevel(request.Level);
        var includeInfrastructure = ResolveIncludeInfrastructure(request.IncludeInfrastructure, requestedLevel);
        var requestedEnvironment = string.IsNullOrWhiteSpace(request.Environment) ? "all" : request.Environment.Trim().ToLowerInvariant();
        var requestedScope = string.IsNullOrWhiteSpace(request.Scope) ? "all" : request.Scope.Trim().ToLowerInvariant();
        var requestedGroupBy = string.IsNullOrWhiteSpace(request.GroupBy) ? "domain" : request.GroupBy.Trim().ToLowerInvariant();

        var projections = graph.Nodes
            .Select(node =>
            {
                var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? "";
                var resolved = GraphClassificationResolver.Resolve(node, resourceGroup);
                var effectiveLevel = ParseResolvedLevel(resolved.C4Level);
                var environment = EnvironmentClassifier.InferEnvironment(node.Name, resourceGroup);
                var domain = GraphDomainClassifier.InferDomain(node.Properties.Domain, node.Name, resourceGroup);
                return new NodeProjection(node, resourceGroup, resolved, effectiveLevel, environment, domain);
            })
            .ToArray();

        var filteredNodes = projections.AsEnumerable();
        if (requestedEnvironment != "all")
            filteredNodes = filteredNodes.Where(n => n.Environment.Equals(requestedEnvironment, StringComparison.OrdinalIgnoreCase));

        if (!includeInfrastructure)
            filteredNodes = filteredNodes.Where(n => !n.Resolved.IsInfrastructure);

        if (requestedLevel.HasValue)
            filteredNodes = filteredNodes.Where(n => n.EffectiveLevel == requestedLevel.Value);

        var filteredList = filteredNodes.ToArray();
        var visibleNodeIds = filteredList.Select(n => n.Node.Id).ToHashSet();
        var filteredEdges = graph.Edges
            .Where(e => visibleNodeIds.Contains(e.SourceNodeId) && visibleNodeIds.Contains(e.TargetNodeId))
            .ToArray();

        if (requestedScope == "corehub")
        {
            ApplyCoreHubScope(ref filteredList, ref filteredEdges, requestedEnvironment);
            visibleNodeIds = filteredList.Select(n => n.Node.Id).ToHashSet();
        }

        var resourceIds = filteredList.Select(n => n.Node.ExternalResourceId).ToArray();

        var healthTask = telemetryQueryService.GetServiceHealthSummariesAsync(request.ProjectId, cancellationToken);
        var driftTask = driftQueryService.GetDriftedResourceIdsAsync(resourceIds, cancellationToken);
        await Task.WhenAll(healthTask, driftTask);

        var healthByService = healthTask.Result.ToDictionary(s => s.Service, s => s, StringComparer.OrdinalIgnoreCase);
        var driftedSet = new HashSet<string>(driftTask.Result, StringComparer.OrdinalIgnoreCase);

        var nodeDtos = filteredList.Select(entry =>
        {
            var node = entry.Node;
            var isDrifted = driftedSet.Contains(node.ExternalResourceId);
            var serviceType = entry.Resolved.ServiceType;
            var groupKey = requestedGroupBy switch
            {
                "resourcegroup" => entry.ResourceGroup,
                "none" => "",
                _ => entry.Domain
            };

            if (healthByService.TryGetValue(node.Name, out var summary))
            {
                return new GraphNodeDto(
                    node.Id.Value,
                    node.Name,
                    node.ExternalResourceId,
                    entry.EffectiveLevel.ToString(),
                    summary.Status.ToLower(),
                    summary.Score,
                    node.ParentId?.Value,
                    isDrifted,
                    entry.Environment,
                    serviceType,
                    entry.ResourceGroup,
                    entry.Domain,
                    entry.Resolved.IsInfrastructure,
                    entry.Resolved.ClassificationSource,
                    entry.Resolved.ClassificationConfidence,
                    groupKey);
            }
            return new GraphNodeDto(
                node.Id.Value,
                node.Name,
                node.ExternalResourceId,
                entry.EffectiveLevel.ToString(),
                "green",
                1.0,
                node.ParentId?.Value,
                isDrifted,
                entry.Environment,
                serviceType,
                entry.ResourceGroup,
                entry.Domain,
                entry.Resolved.IsInfrastructure,
                entry.Resolved.ClassificationSource,
                entry.Resolved.ClassificationConfidence,
                groupKey);
        }).ToArray();

        var healthScoreById = nodeDtos.ToDictionary(n => n.Id, n => n.HealthScore);

        var edgeDtos = filteredEdges.Select(e =>
        {
            var sourceScore = healthScoreById.GetValueOrDefault(e.SourceNodeId.Value, 1.0);
            var targetScore = healthScoreById.GetValueOrDefault(e.TargetNodeId.Value, 1.0);
            var traffic = (sourceScore + targetScore) / 2.0;
            return new GraphEdgeDto(e.Id.Value, e.SourceNodeId.Value, e.TargetNodeId.Value, traffic);
        }).ToArray();

        return Result<GraphDto>.Success(new GraphDto(request.ProjectId, nodeDtos, edgeDtos));
    }

    private static C4Level? ParseLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return null;
        return Enum.TryParse<C4Level>(level, true, out var parsed) ? parsed : null;
    }

    private static C4Level ParseResolvedLevel(string level)
    {
        return Enum.TryParse<C4Level>(level, true, out var parsed) ? parsed : C4Level.Container;
    }

    private static bool ResolveIncludeInfrastructure(string? includeInfrastructure, C4Level? requestedLevel)
    {
        if (string.IsNullOrWhiteSpace(includeInfrastructure) || includeInfrastructure.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return requestedLevel == C4Level.Component;

        return includeInfrastructure.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyCoreHubScope(ref NodeProjection[] filteredList, ref Domain.GraphEdge.GraphEdge[] filteredEdges, string requestedEnvironment)
    {
        bool HasCorePattern(string value)
        {
            var lower = value.ToLowerInvariant();
            return lower.Contains("coreapp", StringComparison.Ordinal)
                || lower.Contains("circit-prod", StringComparison.Ordinal)
                || lower.Contains("app-circit", StringComparison.Ordinal)
                || lower.Contains("circit-stage", StringComparison.Ordinal)
                || lower.Contains("circit-test", StringComparison.Ordinal)
                || lower.Contains("circit-e2e", StringComparison.Ordinal)
                || lower.Contains("circit-trial", StringComparison.Ordinal);
        }

        var anchors = filteredList
            .Where(n =>
                n.Domain.Equals("CoreApp", StringComparison.OrdinalIgnoreCase)
                || HasCorePattern(n.Node.Name)
                || HasCorePattern(n.ResourceGroup))
            .Where(n => requestedEnvironment == "all" || n.Environment.Equals(requestedEnvironment, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (anchors.Length == 0)
            return;

        var visible = anchors.Select(a => a.Node.Id).ToHashSet();
        foreach (var edge in filteredEdges)
        {
            if (visible.Contains(edge.SourceNodeId))
                visible.Add(edge.TargetNodeId);
            if (visible.Contains(edge.TargetNodeId))
                visible.Add(edge.SourceNodeId);
        }

        filteredList = filteredList.Where(n => visible.Contains(n.Node.Id)).ToArray();
        var allowed = filteredList.Select(n => n.Node.Id).ToHashSet();
        filteredEdges = filteredEdges
            .Where(e => allowed.Contains(e.SourceNodeId) && allowed.Contains(e.TargetNodeId))
            .ToArray();
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

    private sealed record NodeProjection(
        Domain.GraphNode.GraphNode Node,
        string ResourceGroup,
        ResolvedNodeClassification Resolved,
        C4Level EffectiveLevel,
        string Environment,
        string Domain);
}
