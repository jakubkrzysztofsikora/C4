using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetThreatAssessment;

public sealed class GetThreatAssessmentHandler(
    IArchitectureGraphRepository repository,
    IThreatDetector threatDetector,
    IProjectAuthorizationService authorizationService
) : IRequestHandler<GetThreatAssessmentQuery, Result<ThreatAssessmentResponse>>
{
    private static readonly TimeSpan DetectorTimeout = TimeSpan.FromSeconds(8);

    public async Task<Result<ThreatAssessmentResponse>> Handle(GetThreatAssessmentQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<ThreatAssessmentResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<ThreatAssessmentResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var classifiedNodes = graph.Nodes
            .Select(node =>
            {
                var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? string.Empty;
                var classification = GraphClassificationResolver.Resolve(node, resourceGroup);
                return new ClassifiedNode(node, classification.ServiceType);
            })
            .ToArray();
        var nodeById = classifiedNodes.ToDictionary(n => n.Node.Id);

        var nodesDescription = string.Join(", ", classifiedNodes.Select(n => $"{n.Node.Name} ({n.Node.Level}, {n.ServiceType})"));
        var edgesDescription = string.Join(", ", graph.Edges.Select(e =>
        {
            var source = nodeById.GetValueOrDefault(e.SourceNodeId);
            var target = nodeById.GetValueOrDefault(e.TargetNodeId);
            return source is not null && target is not null
                ? $"{source.Node.Name} -> {target.Node.Name}"
                : $"{e.SourceNodeId} -> {e.TargetNodeId}";
        }));

        ThreatDetectionResult result;
        try
        {
            var detectorTask = threatDetector.DetectThreatsAsync(
                request.ProjectId,
                nodesDescription,
                edgesDescription,
                request.View,
                cancellationToken);
            var timeoutTask = Task.Delay(DetectorTimeout, cancellationToken);
            var completed = await Task.WhenAny(detectorTask, timeoutTask);

            result = completed == detectorTask
                ? await detectorTask
                : BuildDeterministicThreats(classifiedNodes, graph.Edges, request.View);
        }
        catch
        {
            result = BuildDeterministicThreats(classifiedNodes, graph.Edges, request.View);
        }

        return Result<ThreatAssessmentResponse>.Success(
            new ThreatAssessmentResponse(
                request.ProjectId,
                result.RiskLevel,
                result.Threats,
                DataProvenance: "heuristic",
                GeneratedAtUtc: DateTime.UtcNow,
                IsHeuristic: true));
    }

    private static ThreatDetectionResult BuildDeterministicThreats(
        IReadOnlyCollection<ClassifiedNode> nodes,
        IReadOnlyCollection<Domain.GraphEdge.GraphEdge> edges,
        string? view)
    {
        var normalizedView = NormalizeView(view);
        List<ThreatItem> threats = normalizedView switch
        {
            "api-attack-surface" => BuildApiAttackSurfaceThreats(nodes),
            "exit-points" => BuildExitPointThreats(nodes),
            "data-exposure" => BuildDataExposureThreats(nodes),
            "blast-radius" => BuildBlastRadiusThreats(nodes, edges),
            _ => BuildGeneralThreats(nodes, edges)
        };

        var risk = ResolveRiskLevel(threats);
        return new ThreatDetectionResult(risk, threats);
    }

    private static string NormalizeView(string? view)
    {
        if (string.IsNullOrWhiteSpace(view)) return "general";
        var lower = view.Trim().ToLowerInvariant();
        if (lower is "api" or "apiattacksurface") return "api-attack-surface";
        if (lower is "exit" or "exitpoints") return "exit-points";
        if (lower is "data" or "dataexposure") return "data-exposure";
        if (lower is "blast" or "blastradius") return "blast-radius";
        return lower;
    }

    private static List<ThreatItem> BuildGeneralThreats(
        IReadOnlyCollection<ClassifiedNode> nodes,
        IReadOnlyCollection<Domain.GraphEdge.GraphEdge> edges)
    {
        List<ThreatItem> threats = [];
        threats.AddRange(BuildApiAttackSurfaceThreats(nodes).Take(6));
        threats.AddRange(BuildDataExposureThreats(nodes).Take(6));
        threats.AddRange(BuildExitPointThreats(nodes).Take(4));
        threats.AddRange(BuildBlastRadiusThreats(nodes, edges).Take(4));
        return threats.DistinctBy(t => (t.Component, t.ThreatType, t.Severity)).Take(20).ToList();
    }

    private static List<ThreatItem> BuildApiAttackSurfaceThreats(IReadOnlyCollection<ClassifiedNode> nodes)
    {
        return nodes
            .Where(n => n.ServiceType is "api" or "app" or "external")
            .Take(20)
            .Select(n => new ThreatItem(
                n.Node.Name,
                "API Attack Surface",
                "High",
                "Enforce strong authentication, rate limits, and request validation on public-facing endpoints.",
                "heuristic",
                true))
            .ToList();
    }

    private static List<ThreatItem> BuildExitPointThreats(IReadOnlyCollection<ClassifiedNode> nodes)
    {
        return nodes
            .Where(n => n.ServiceType is "external" or "boundary")
            .Take(20)
            .Select(n => new ThreatItem(
                n.Node.Name,
                "Data Egress / Exit Point",
                "Medium",
                "Review outbound connectivity, destination allowlists, and egress monitoring controls.",
                "heuristic",
                true))
            .ToList();
    }

    private static List<ThreatItem> BuildDataExposureThreats(IReadOnlyCollection<ClassifiedNode> nodes)
    {
        return nodes
            .Where(n => n.ServiceType is "database" or "storage")
            .Take(20)
            .Select(n => new ThreatItem(
                n.Node.Name,
                "Sensitive Data Exposure",
                "Critical",
                "Apply encryption-at-rest/in-transit, strict IAM policies, key rotation, and audit logging.",
                "heuristic",
                true))
            .ToList();
    }

    private static List<ThreatItem> BuildBlastRadiusThreats(
        IReadOnlyCollection<ClassifiedNode> nodes,
        IReadOnlyCollection<Domain.GraphEdge.GraphEdge> edges)
    {
        var degreeByNode = edges
            .GroupBy(e => e.SourceNodeId)
            .ToDictionary(g => g.Key, g => g.Count());

        return nodes
            .Select(n => new { n.Node.Name, Degree = degreeByNode.GetValueOrDefault(n.Node.Id, 0) })
            .OrderByDescending(n => n.Degree)
            .Take(12)
            .Where(n => n.Degree > 0)
            .Select(n => new ThreatItem(
                n.Name,
                "Blast Radius Concentration",
                n.Degree >= 10 ? "High" : "Medium",
                "Segment trust boundaries and isolate high-connectivity services to reduce lateral movement impact.",
                "heuristic",
                true))
            .ToList();
    }

    private static string ResolveRiskLevel(IReadOnlyCollection<ThreatItem> threats)
    {
        if (threats.Any(t => t.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))) return "Critical";
        if (threats.Any(t => t.Severity.Equals("High", StringComparison.OrdinalIgnoreCase))) return "High";
        if (threats.Any(t => t.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase))) return "Medium";
        return "Low";
    }

    private static string? ExtractResourceGroup(string resourceId)
    {
        var lower = resourceId.ToLowerInvariant();
        var rgIndex = lower.IndexOf("/resourcegroups/", StringComparison.Ordinal);
        if (rgIndex < 0) return null;
        var start = rgIndex + "/resourcegroups/".Length;
        var end = lower.IndexOf('/', start);
        return end < 0 ? lower[start..] : lower[start..end];
    }

    private sealed record ClassifiedNode(Domain.GraphNode.GraphNode Node, string ServiceType);
}
