using System.Text;
using C4.Modules.Graph.Application.Ports;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace C4.Modules.Graph.Infrastructure.AI;

public sealed class ArchitectureAnalysisPlugin(
    Kernel kernel,
    ILearningProvider? learningProvider = null,
    IProjectArchitectureContextProvider? architectureContextProvider = null,
    ILogger<ArchitectureAnalysisPlugin>? logger = null) : IArchitectureAnalyzer
{
    public async Task<ArchitectureAnalysisResult> AnalyzeAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
    {
        var contextSection = await BuildArchitectureContextSectionAsync(projectId, cancellationToken);
        var learningsSection = await BuildLearningsSectionAsync(projectId, cancellationToken);

        var prompt = $$"""
            Analyze the following architecture and provide a brief summary and recommendations.

            Nodes: {{nodesDescription}}
            Edges: {{edgesDescription}}
            {{contextSection}}
            {{learningsSection}}
            Respond with:
            1. A one-paragraph summary of the architecture
            2. Up to 5 specific recommendations for improvement

            Format your response as:
            SUMMARY: <your summary>
            RECOMMENDATIONS:
            - <recommendation 1>
            - <recommendation 2>
            """;

        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var text = result.GetValue<string>() ?? string.Empty;

        return ParseAnalysisResult(text);
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
            var learnings = await learningProvider.GetActiveLearningsAsync(projectId, "ArchitectureAnalysis", cancellationToken);
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

    private static ArchitectureAnalysisResult ParseAnalysisResult(string text)
    {
        var summary = "Architecture analysis completed.";
        var recommendations = new List<string>();

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var inRecommendations = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
            {
                summary = trimmed["SUMMARY:".Length..].Trim();
            }
            else if (trimmed.StartsWith("RECOMMENDATIONS:", StringComparison.OrdinalIgnoreCase))
            {
                inRecommendations = true;
            }
            else if (inRecommendations && trimmed.StartsWith('-'))
            {
                recommendations.Add(trimmed[1..].Trim());
            }
        }

        if (recommendations.Count == 0 && !string.IsNullOrWhiteSpace(text))
        {
            summary = text.Length > 500 ? text[..500] : text;
            recommendations.Add("Review the architecture for potential improvements.");
        }

        return new ArchitectureAnalysisResult(summary, recommendations);
    }
}
