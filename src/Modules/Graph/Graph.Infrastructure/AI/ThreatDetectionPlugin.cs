using System.Text;
using C4.Modules.Graph.Application.GetThreatAssessment;
using C4.Modules.Graph.Application.Ports;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace C4.Modules.Graph.Infrastructure.AI;

public sealed class ThreatDetectionPlugin(
    Kernel kernel,
    ILearningProvider? learningProvider = null,
    IProjectArchitectureContextProvider? architectureContextProvider = null,
    ILogger<ThreatDetectionPlugin>? logger = null) : IThreatDetector
{
    public async Task<ThreatDetectionResult> DetectThreatsAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
    {
        var contextSection = await BuildArchitectureContextSectionAsync(projectId, cancellationToken);
        var learningsSection = await BuildLearningsSectionAsync(projectId, cancellationToken);

        var prompt = $$"""
            Analyze the following architecture for security threats using STRIDE methodology.

            Nodes: {{nodesDescription}}
            Edges: {{edgesDescription}}
            {{contextSection}}
            {{learningsSection}}
            For each threat found, provide:
            - Component affected
            - Threat type (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege)
            - Severity (Low, Medium, High, Critical)
            - Recommended mitigation

            Format your response as:
            RISK_LEVEL: <Low|Medium|High|Critical>
            THREATS:
            - COMPONENT: <name> | TYPE: <type> | SEVERITY: <severity> | MITIGATION: <mitigation>
            """;

        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var text = result.GetValue<string>() ?? string.Empty;

        return ParseThreatResult(text);
    }

    private async Task<string> BuildArchitectureContextSectionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (architectureContextProvider is null)
            return string.Empty;

        try
        {
            var context = await architectureContextProvider.GetActiveContextAsync(projectId, cancellationToken);
            if (context is null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Approved project architecture context:");
            sb.AppendLine($"- Description: {context.ProjectDescription}");
            sb.AppendLine($"- Boundaries: {context.SystemBoundaries}");
            sb.AppendLine($"- Core domains: {context.CoreDomains}");
            sb.AppendLine($"- External dependencies: {context.ExternalDependencies}");
            sb.AppendLine($"- Data sensitivity: {context.DataSensitivity}");
            if (context.ApprovedQuestions.Count > 0)
            {
                sb.AppendLine("Approved clarifying Q&A:");
                foreach (var qa in context.ApprovedQuestions.Take(10))
                {
                    sb.AppendLine($"- Q: {qa.Question}");
                    sb.AppendLine($"  A: {qa.Answer}");
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to fetch project architecture context");
            return string.Empty;
        }
    }

    private async Task<string> BuildLearningsSectionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (learningProvider is null)
            return string.Empty;

        try
        {
            var learnings = await learningProvider.GetActiveLearningsAsync(projectId, "ThreatAssessment", cancellationToken);
            if (learnings.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Previous user feedback learnings to incorporate:");
            foreach (var learning in learnings.Take(10))
            {
                sb.AppendLine($"- [{learning.InsightType}] {learning.Description} (confidence: {learning.Confidence:F2})");
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to fetch learnings for prompt augmentation");
            return string.Empty;
        }
    }

    private static ThreatDetectionResult ParseThreatResult(string text)
    {
        var riskLevel = "Low";
        var threats = new List<ThreatItem>();

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("RISK_LEVEL:", StringComparison.OrdinalIgnoreCase))
            {
                riskLevel = trimmed["RISK_LEVEL:".Length..].Trim();
            }
            else if (trimmed.StartsWith("- COMPONENT:", StringComparison.OrdinalIgnoreCase))
            {
                var threat = ParseThreatLine(trimmed);
                if (threat is not null) threats.Add(threat);
            }
        }

        return new ThreatDetectionResult(riskLevel, threats);
    }

    private static ThreatItem? ParseThreatLine(string line)
    {
        var parts = line.Split('|');
        if (parts.Length < 4) return null;

        static string ExtractValue(string part)
        {
            var colonIndex = part.IndexOf(':');
            return colonIndex >= 0 ? part[(colonIndex + 1)..].Trim() : part.Trim();
        }

        return new ThreatItem(
            ExtractValue(parts[0]),
            ExtractValue(parts[1]),
            ExtractValue(parts[2]),
            ExtractValue(parts[3]));
    }
}
