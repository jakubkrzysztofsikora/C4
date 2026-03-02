using C4.Modules.Graph.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
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

        var snapshot = request.SnapshotId.HasValue
            ? graph.Snapshots.FirstOrDefault(s => s.Id.Value == request.SnapshotId.Value)
            : null;
        if (snapshot is not null && IsEmptySnapshot(snapshot))
            snapshot = null;

        var sourceNodes = BuildWorkingNodes(graph, snapshot);
        var sourceEdges = BuildWorkingEdges(graph, snapshot);

        C4Level? requestedLevel = ParseLevel(request.Level);
        var includeInfrastructure = ResolveIncludeInfrastructure(request.IncludeInfrastructure, requestedLevel);
        var requestedEnvironment = string.IsNullOrWhiteSpace(request.Environment) ? "all" : request.Environment.Trim().ToLowerInvariant();
        var requestedScope = string.IsNullOrWhiteSpace(request.Scope) ? "all" : request.Scope.Trim().ToLowerInvariant();
        var requestedGroupBy = string.IsNullOrWhiteSpace(request.GroupBy) ? "domain" : request.GroupBy.Trim().ToLowerInvariant();

        var projections = sourceNodes
            .Select(node =>
            {
                var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? "";
                var environment = EnvironmentClassifier.InferEnvironment(node.Name, resourceGroup);
                var domain = GraphDomainClassifier.InferDomain(node.Domain, node.Name, resourceGroup);
                return new NodeProjection(node, resourceGroup, node.Level, environment, domain);
            })
            .ToArray();

        var filteredNodes = projections.AsEnumerable();
        if (requestedEnvironment != "all")
            filteredNodes = filteredNodes.Where(n => n.Environment.Equals(requestedEnvironment, StringComparison.OrdinalIgnoreCase));

        if (!includeInfrastructure)
            filteredNodes = filteredNodes.Where(n => !n.Node.IsInfrastructure);

        if (requestedLevel.HasValue)
            filteredNodes = filteredNodes.Where(n => n.EffectiveLevel == requestedLevel.Value);

        var filteredList = filteredNodes.ToArray();
        var visibleNodeIds = filteredList.Select(n => n.Node.Id).ToHashSet();
        var filteredEdges = sourceEdges
            .Where(e => visibleNodeIds.Contains(e.SourceNodeId) && visibleNodeIds.Contains(e.TargetNodeId))
            .ToArray();

        if (requestedScope == "corehub")
        {
            ApplyCoreHubScope(ref filteredList, ref filteredEdges, requestedEnvironment);
            visibleNodeIds = filteredList.Select(n => n.Node.Id).ToHashSet();
        }

        var resourceIds = filteredList.Select(n => n.Node.ExternalResourceId).ToArray();

        var healthTask = telemetryQueryService.GetServiceHealthSummariesAsync(request.ProjectId, cancellationToken);
        var dependencyTask = telemetryQueryService.GetDependencySummariesAsync(request.ProjectId, cancellationToken);
        var driftTask = driftQueryService.GetDriftedResourceIdsAsync(resourceIds, cancellationToken);
        await Task.WhenAll(healthTask, driftTask, dependencyTask);

        var healthByService = healthTask.Result.ToDictionary(s => s.Service, s => s, StringComparer.OrdinalIgnoreCase);
        var healthByNormalized = healthTask.Result
            .GroupBy(s => NormalizeTelemetryIdentifier(s.Service))
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.RequestRate ?? 0).First(), StringComparer.Ordinal);
        var dependencyHealthByNormalized = BuildDerivedHealthFromDependencies(dependencyTask.Result);
        var edgeTelemetry = BuildEdgeTelemetryIndex(dependencyTask.Result);
        var driftedSet = new HashSet<string>(driftTask.Result, StringComparer.OrdinalIgnoreCase);

        var nodeDtos = filteredList.Select(entry =>
        {
            var node = entry.Node;
            var isDrifted = driftedSet.Contains(node.ExternalResourceId);
            var riskLevel = ResolveRiskLevel(node.ServiceType, node.IsInfrastructure, isDrifted);
            var hourlyCostUsd = EstimateHourlyCost(node.ServiceType, entry.EffectiveLevel, node.IsInfrastructure);
            var groupKey = requestedGroupBy switch
            {
                "resourcegroup" => entry.ResourceGroup,
                "none" => "",
                _ => entry.Domain
            };

            if (TryResolveNodeHealth(
                    node.Name,
                    node.ExternalResourceId,
                    healthByService,
                    healthByNormalized,
                    dependencyHealthByNormalized,
                    out var summary))
            {
                return new GraphNodeDto(
                    node.Id,
                    node.Name,
                    node.ExternalResourceId,
                    entry.EffectiveLevel.ToString(),
                    summary.Status.ToLowerInvariant(),
                    summary.Score,
                    summary.TelemetryStatus,
                    summary.RequestRate,
                    summary.ErrorRate,
                    summary.P95LatencyMs,
                    riskLevel,
                    hourlyCostUsd,
                    node.ParentId,
                    isDrifted,
                    entry.Environment,
                    node.ServiceType,
                    node.Technology,
                    entry.ResourceGroup,
                    entry.Domain,
                    node.IsInfrastructure,
                    node.ClassificationSource,
                    node.ClassificationConfidence,
                    groupKey);
            }

            return new GraphNodeDto(
                node.Id,
                node.Name,
                node.ExternalResourceId,
                entry.EffectiveLevel.ToString(),
                "unknown",
                0,
                "unknown",
                null,
                null,
                null,
                riskLevel,
                hourlyCostUsd,
                node.ParentId,
                isDrifted,
                entry.Environment,
                node.ServiceType,
                node.Technology,
                entry.ResourceGroup,
                entry.Domain,
                node.IsInfrastructure,
                node.ClassificationSource,
                node.ClassificationConfidence,
                groupKey);
        }).ToArray();

        var nodeById = nodeDtos.ToDictionary(n => n.Id, n => n);

        var edgeDtos = filteredEdges.Select(e =>
        {
            var sourceNode = nodeById.GetValueOrDefault(e.SourceNodeId);
            var targetNode = nodeById.GetValueOrDefault(e.TargetNodeId);

            var dependencySummary = TryResolveEdgeTelemetry(sourceNode, targetNode, edgeTelemetry);
            var requestRate = dependencySummary?.RequestRate ?? AverageNullable(sourceNode?.RequestRate, targetNode?.RequestRate);
            var errorRate = dependencySummary?.ErrorRate ?? AverageNullable(sourceNode?.ErrorRate, targetNode?.ErrorRate);
            var p95LatencyMs = dependencySummary?.P95LatencyMs ?? AverageNullable(sourceNode?.P95LatencyMs, targetNode?.P95LatencyMs);
            var traffic = ResolveTrafficScore(requestRate, errorRate, p95LatencyMs);
            var trafficState = ResolveTrafficState(requestRate, errorRate, p95LatencyMs);
            var protocol = dependencySummary?.Protocol ?? e.Protocol;

            return new GraphEdgeDto(
                e.Id,
                e.SourceNodeId,
                e.TargetNodeId,
                traffic,
                trafficState,
                requestRate,
                errorRate,
                p95LatencyMs,
                protocol);
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

    private static string ResolveTrafficState(double? requestRate, double? errorRate, double? p95LatencyMs)
    {
        if (errorRate is not null)
        {
            if (errorRate >= 0.05) return "red";
            if (errorRate >= 0.01) return "yellow";
            return "green";
        }

        if (p95LatencyMs is not null)
        {
            if (p95LatencyMs >= 2000) return "red";
            if (p95LatencyMs >= 800) return "yellow";
            return "green";
        }

        if (requestRate is not null)
        {
            if (requestRate >= 100) return "green";
            if (requestRate >= 25) return "yellow";
            return "red";
        }

        return "unknown";
    }

    private static double ResolveTrafficScore(double? requestRate, double? errorRate, double? p95LatencyMs)
    {
        if (requestRate is null && errorRate is null && p95LatencyMs is null)
            return 0;

        var normalizedRequestRate = requestRate is null ? 0.5 : Math.Clamp(requestRate.Value / 100.0, 0, 1);
        var normalizedErrorPenalty = errorRate is null ? 0.5 : Math.Clamp(1 - (errorRate.Value / 0.1), 0, 1);
        var normalizedLatencyPenalty = p95LatencyMs is null ? 0.5 : Math.Clamp(1 - (p95LatencyMs.Value / 2000.0), 0, 1);

        return Math.Round((normalizedRequestRate + normalizedErrorPenalty + normalizedLatencyPenalty) / 3.0, 2);
    }

    private static double? AverageNullable(double? first, double? second)
    {
        if (first is null && second is null) return null;
        if (first is null) return second;
        if (second is null) return first;
        return (first.Value + second.Value) / 2.0;
    }

    private static string ResolveRiskLevel(string serviceType, bool isInfrastructure, bool isDrifted)
    {
        if (isDrifted) return "high";
        if (isInfrastructure) return "low";
        return serviceType switch
        {
            "database" => "high",
            "storage" => "high",
            "api" => "medium",
            "queue" => "medium",
            "cache" => "medium",
            _ => "low"
        };
    }

    private static double EstimateHourlyCost(string serviceType, C4Level level, bool isInfrastructure)
    {
        if (isInfrastructure) return 0.02;

        var baseCost = serviceType switch
        {
            "database" => 1.25,
            "storage" => 0.32,
            "api" => 0.18,
            "app" => 0.24,
            "queue" => 0.10,
            "cache" => 0.46,
            "monitoring" => 0.12,
            _ => 0.08
        };

        return level switch
        {
            C4Level.Context => Math.Round(baseCost * 0.5, 2),
            C4Level.Component => Math.Round(baseCost * 0.75, 2),
            _ => Math.Round(baseCost, 2)
        };
    }

    private static bool ResolveIncludeInfrastructure(string? includeInfrastructure, C4Level? requestedLevel)
    {
        if (string.IsNullOrWhiteSpace(includeInfrastructure) || includeInfrastructure.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return requestedLevel == C4Level.Component;

        return includeInfrastructure.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmptySnapshot(Domain.GraphSnapshot.GraphSnapshot snapshot)
        => snapshot.Nodes.Count == 0 && snapshot.Edges.Count == 0;

    private static WorkingNode[] BuildWorkingNodes(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        Domain.GraphSnapshot.GraphSnapshot? snapshot)
    {
        if (snapshot is not null)
        {
            return snapshot.Nodes
                .Select(node => new WorkingNode(
                    node.Id,
                    node.ExternalResourceId,
                    node.Name,
                    ParseResolvedLevel(node.Level),
                    node.ParentId,
                    ResolveSnapshotServiceType(node.ServiceType, node.ExternalResourceId),
                    ResolveTechnologyHint(node.ExternalResourceId, node.ServiceType),
                    string.IsNullOrWhiteSpace(node.Domain) ? "General" : node.Domain,
                    node.IsInfrastructure,
                    string.IsNullOrWhiteSpace(node.ClassificationSource) ? "snapshot" : node.ClassificationSource,
                    node.ClassificationConfidence <= 0 ? 0.7 : node.ClassificationConfidence))
                .ToArray();
        }

        return graph.Nodes
            .Select(node =>
            {
                var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? "";
                var resolved = GraphClassificationResolver.Resolve(node, resourceGroup);
                var effectiveLevel = ParseResolvedLevel(resolved.C4Level);
                var domain = GraphDomainClassifier.InferDomain(node.Properties.Domain, node.Name, resourceGroup);

                return new WorkingNode(
                    node.Id.Value,
                    node.ExternalResourceId,
                    node.Name,
                    effectiveLevel,
                    node.ParentId?.Value,
                    resolved.ServiceType,
                    ResolveTechnologyHint(node.ExternalResourceId, node.Properties.Technology),
                    domain,
                    resolved.IsInfrastructure,
                    resolved.ClassificationSource,
                    resolved.ClassificationConfidence);
            })
            .ToArray();
    }

    private static WorkingEdge[] BuildWorkingEdges(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        Domain.GraphSnapshot.GraphSnapshot? snapshot)
    {
        if (snapshot is not null)
        {
            return snapshot.Edges
                .Select(edge => new WorkingEdge(edge.Id, edge.SourceNodeId, edge.TargetNodeId, edge.Protocol))
                .ToArray();
        }

        return graph.Edges
            .Select(e => new WorkingEdge(e.Id.Value, e.SourceNodeId.Value, e.TargetNodeId.Value, e.Properties.Protocol))
            .ToArray();
    }

    private static void ApplyCoreHubScope(ref NodeProjection[] filteredList, ref WorkingEdge[] filteredEdges, string requestedEnvironment)
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

    private static string ResolveSnapshotServiceType(string? snapshotServiceType, string externalResourceId)
    {
        if (!string.IsNullOrWhiteSpace(snapshotServiceType)
            && !snapshotServiceType.Equals("n/a", StringComparison.OrdinalIgnoreCase)
            && !snapshotServiceType.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return snapshotServiceType;
        }

        var armType = GraphClassificationResolver.ExtractArmType(externalResourceId);
        var classification = AzureResourceTypeCatalog.Classify(armType);
        return classification.ServiceType;
    }

    private static string ResolveTechnologyHint(string externalResourceId, string? explicitTechnology)
    {
        if (!string.IsNullOrWhiteSpace(explicitTechnology) && !explicitTechnology.Equals("n/a", StringComparison.OrdinalIgnoreCase))
            return explicitTechnology;

        var lower = externalResourceId.ToLowerInvariant();
        if (lower.Contains("/providers/microsoft.web/sites/", StringComparison.Ordinal)) return "Azure App Service";
        if (lower.Contains("/providers/microsoft.sql/", StringComparison.Ordinal)) return "Azure SQL";
        if (lower.Contains("/providers/microsoft.storage/", StringComparison.Ordinal)) return "Azure Storage";
        if (lower.Contains("/providers/microsoft.cache/redis/", StringComparison.Ordinal)) return "Azure Cache for Redis";
        if (lower.Contains("/providers/microsoft.insights/components/", StringComparison.Ordinal)) return "Application Insights";
        if (lower.Contains("/providers/microsoft.servicebus/", StringComparison.Ordinal)) return "Azure Service Bus";
        if (lower.Contains("/providers/microsoft.network/", StringComparison.Ordinal)) return "Azure Networking";

        var providerIndex = lower.IndexOf("/providers/", StringComparison.Ordinal);
        if (providerIndex >= 0)
        {
            var start = providerIndex + "/providers/".Length;
            var end = lower.IndexOf('/', start);
            if (end > start)
            {
                var provider = lower[start..end];
                return provider.Replace(".", " ", StringComparison.Ordinal).ToUpperInvariant();
            }
        }

        return "unknown";
    }

    private static EdgeTelemetryIndex BuildEdgeTelemetryIndex(IReadOnlyCollection<ServiceDependencySummary> dependencies)
    {
        Dictionary<string, ServiceDependencySummary> exact = new(StringComparer.Ordinal);
        Dictionary<string, List<ServiceDependencySummary>> bySource = new(StringComparer.Ordinal);

        foreach (var item in dependencies.Where(d => d.TelemetryStatus.Equals("known", StringComparison.OrdinalIgnoreCase)))
        {
            var source = NormalizeTelemetryIdentifier(item.SourceService);
            var target = NormalizeTelemetryIdentifier(item.TargetService);
            if (source.Length == 0 || target.Length == 0) continue;

            var key = $"{source}->{target}";
            if (!exact.TryGetValue(key, out var existing))
            {
                exact[key] = item;
            }
            else
            {
                exact[key] = new ServiceDependencySummary(
                    item.SourceService,
                    item.TargetService,
                    (existing.RequestRate + item.RequestRate) / 2.0,
                    (existing.ErrorRate + item.ErrorRate) / 2.0,
                    (existing.P95LatencyMs + item.P95LatencyMs) / 2.0,
                    item.Protocol ?? existing.Protocol,
                    item.TelemetryStatus);
            }

            if (!bySource.TryGetValue(source, out var list))
            {
                list = [];
                bySource[source] = list;
            }
            list.Add(item);
        }

        return new EdgeTelemetryIndex(exact, bySource);
    }

    private static bool TryResolveNodeHealth(
        string nodeName,
        string externalResourceId,
        IReadOnlyDictionary<string, ServiceHealthSummary> byName,
        IReadOnlyDictionary<string, ServiceHealthSummary> byNormalized,
        IReadOnlyDictionary<string, ServiceHealthSummary> dependencyDerivedByNormalized,
        out ServiceHealthSummary summary)
    {
        if (byName.TryGetValue(nodeName, out summary!))
            return true;

        foreach (var candidate in BuildTelemetryCandidates(nodeName, externalResourceId))
        {
            if (byNormalized.TryGetValue(candidate, out summary!))
                return true;
            if (dependencyDerivedByNormalized.TryGetValue(candidate, out summary!))
                return true;
        }

        summary = default!;
        return false;
    }

    private static IReadOnlyDictionary<string, ServiceHealthSummary> BuildDerivedHealthFromDependencies(
        IReadOnlyCollection<ServiceDependencySummary> dependencies)
    {
        return dependencies
            .Where(d => d.TelemetryStatus.Equals("known", StringComparison.OrdinalIgnoreCase))
            .SelectMany(d => new[]
            {
                new { Key = NormalizeTelemetryIdentifier(d.SourceService), d.RequestRate, d.ErrorRate, d.P95LatencyMs },
                new { Key = NormalizeTelemetryIdentifier(d.TargetService), d.RequestRate, d.ErrorRate, d.P95LatencyMs }
            })
            .Where(x => x.Key.Length > 0)
            .GroupBy(x => x.Key)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var requestRate = g.Average(x => x.RequestRate);
                    var errorRate = g.Average(x => x.ErrorRate);
                    var p95LatencyMs = g.Average(x => x.P95LatencyMs);
                    var score = Math.Clamp(1.0 - errorRate, 0, 1);

                    return new ServiceHealthSummary(
                        g.Key,
                        score,
                        ResolveTrafficState(requestRate, errorRate, p95LatencyMs),
                        RequestRate: requestRate,
                        ErrorRate: errorRate,
                        P95LatencyMs: p95LatencyMs,
                        TelemetryStatus: "known");
                },
                StringComparer.Ordinal);
    }

    private static ServiceDependencySummary? TryResolveEdgeTelemetry(
        GraphNodeDto? sourceNode,
        GraphNodeDto? targetNode,
        EdgeTelemetryIndex index)
    {
        if (sourceNode is null || targetNode is null)
            return null;

        var sourceCandidates = BuildTelemetryCandidates(sourceNode.Name, sourceNode.ExternalResourceId);
        var targetCandidates = BuildTelemetryCandidates(targetNode.Name, targetNode.ExternalResourceId);

        foreach (var source in sourceCandidates)
        {
            foreach (var target in targetCandidates)
            {
                var key = $"{source}->{target}";
                if (index.Exact.TryGetValue(key, out var match))
                    return match;
            }
        }

        foreach (var source in sourceCandidates)
        {
            if (!index.BySource.TryGetValue(source, out var list))
                continue;

            foreach (var candidate in list)
            {
                var normalizedTarget = NormalizeTelemetryIdentifier(candidate.TargetService);
                if (targetCandidates.Any(t =>
                        normalizedTarget.Contains(t, StringComparison.Ordinal)
                        || t.Contains(normalizedTarget, StringComparison.Ordinal)))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static IReadOnlyCollection<string> BuildTelemetryCandidates(string name, string externalResourceId)
    {
        HashSet<string> values = [NormalizeTelemetryIdentifier(name)];

        var slashParts = externalResourceId
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in slashParts.TakeLast(4))
        {
            var normalized = NormalizeTelemetryIdentifier(part);
            if (normalized.Length > 0)
                values.Add(normalized);
        }

        return values.Where(v => v.Length > 0).ToArray();
    }

    private static string NormalizeTelemetryIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var lower = value.Trim().ToLowerInvariant();
        Span<char> buffer = stackalloc char[lower.Length];
        var index = 0;
        foreach (var ch in lower)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                buffer[index++] = ch;
        }

        return new string(buffer[..index]);
    }

    private sealed record WorkingNode(
        Guid Id,
        string ExternalResourceId,
        string Name,
        C4Level Level,
        Guid? ParentId,
        string ServiceType,
        string Technology,
        string Domain,
        bool IsInfrastructure,
        string ClassificationSource,
        double ClassificationConfidence);

    private sealed record WorkingEdge(Guid Id, Guid SourceNodeId, Guid TargetNodeId, string Protocol);

    private sealed record NodeProjection(
        WorkingNode Node,
        string ResourceGroup,
        C4Level EffectiveLevel,
        string Environment,
        string Domain);

    private sealed record EdgeTelemetryIndex(
        IReadOnlyDictionary<string, ServiceDependencySummary> Exact,
        IReadOnlyDictionary<string, List<ServiceDependencySummary>> BySource);
}
