using System.Text.Json;
using System.Text;
using C4.Modules.Graph.Application.GetThreatAssessment;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.ExportArchitectureBundle;

public sealed record ExportArchitectureBundleCommand(Guid ProjectId) : IRequest<Result<ExportArchitectureBundleResponse>>;

public sealed record ExportArchitectureBundleResponse(
    Guid ProjectId,
    string ArchitectureSummaryMarkdown,
    string ThreatModelMarkdown,
    string BundleJson);

public sealed class ExportArchitectureBundleHandler(
    IArchitectureGraphRepository repository,
    IArchitectureAnalyzer analyzer,
    IThreatDetector threatDetector,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<ExportArchitectureBundleCommand, Result<ExportArchitectureBundleResponse>>
{
    public async Task<Result<ExportArchitectureBundleResponse>> Handle(ExportArchitectureBundleCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<ExportArchitectureBundleResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<ExportArchitectureBundleResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var nodesDescription = JsonSerializer.Serialize(graph.Nodes.Select(n => new { n.Name, Level = n.Level.ToString(), Technology = n.Properties.Technology }));
        var edgesDescription = JsonSerializer.Serialize(graph.Edges.Select(e => new { e.SourceNodeId, e.TargetNodeId }));

        var analysis = await analyzer.AnalyzeAsync(request.ProjectId, nodesDescription, edgesDescription, cancellationToken);
        var threatResult = await threatDetector.DetectThreatsAsync(
            request.ProjectId,
            string.Join(", ", graph.Nodes.Select(n => $"{n.Name} ({n.Level})")),
            string.Join(", ", graph.Edges.Select(e => $"{e.SourceNodeId} -> {e.TargetNodeId}")),
            view: null,
            cancellationToken);

        string architectureMarkdown = BuildArchitectureMarkdown(request.ProjectId, analysis);
        string threatMarkdown = BuildThreatMarkdown(request.ProjectId, threatResult);

        string bundleJson = JsonSerializer.Serialize(new
        {
            projectId = request.ProjectId,
            generatedAtUtc = DateTime.UtcNow,
            diagram = new
            {
                nodes = graph.Nodes.Select(n => new
                {
                    id = n.Id.Value,
                    n.Name,
                    level = n.Level.ToString(),
                    serviceType = n.Properties.Technology,
                    domain = n.Properties.Domain,
                    isInfrastructure = n.Properties.IsInfrastructure
                }),
                edges = graph.Edges.Select(e => new
                {
                    id = e.Id.Value,
                    sourceNodeId = e.SourceNodeId.Value,
                    targetNodeId = e.TargetNodeId.Value
                })
            },
            analysis = new
            {
                summary = analysis.Summary,
                recommendations = analysis.Recommendations
            },
            threatModel = new
            {
                riskLevel = threatResult.RiskLevel,
                threats = threatResult.Threats
            },
            assumptions = new[]
            {
                "Diagram was generated from discovered Azure resources and inferred relationships.",
                "Threat model follows STRIDE-oriented analysis of visible components and data flows."
            }
        });

        return Result<ExportArchitectureBundleResponse>.Success(
            new ExportArchitectureBundleResponse(request.ProjectId, architectureMarkdown, threatMarkdown, bundleJson));
    }

    private static string BuildArchitectureMarkdown(Guid projectId, ArchitectureAnalysisResult analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Architecture Summary");
        sb.AppendLine();
        sb.AppendLine($"Project: `{projectId}`");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine(analysis.Summary);
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        foreach (var recommendation in analysis.Recommendations)
        {
            sb.AppendLine($"- {recommendation}");
        }

        return sb.ToString();
    }

    private static string BuildThreatMarkdown(Guid projectId, ThreatDetectionResult threat)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Threat Model");
        sb.AppendLine();
        sb.AppendLine($"Project: `{projectId}`");
        sb.AppendLine();
        sb.AppendLine($"Overall Risk Level: **{threat.RiskLevel}**");
        sb.AppendLine();
        sb.AppendLine("## Threats");
        if (threat.Threats.Count == 0)
        {
            sb.AppendLine("- No explicit threats returned by detector.");
            return sb.ToString();
        }

        foreach (var item in threat.Threats)
        {
            sb.AppendLine($"- **Component:** {item.Component}");
            sb.AppendLine($"  **Type:** {item.ThreatType}; **Severity:** {item.Severity}");
            sb.AppendLine($"  **Mitigation:** {item.Mitigation}");
        }

        return sb.ToString();
    }
}
