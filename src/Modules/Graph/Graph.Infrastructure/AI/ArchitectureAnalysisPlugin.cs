using C4.Modules.Graph.Application.Ports;
using Microsoft.SemanticKernel;

namespace C4.Modules.Graph.Infrastructure.AI;

public sealed class ArchitectureAnalysisPlugin(Kernel kernel) : IArchitectureAnalyzer
{
    public async Task<ArchitectureAnalysisResult> AnalyzeAsync(string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
    {
        var prompt = $$"""
            Analyze the following architecture and provide a brief summary and recommendations.

            Nodes: {{nodesDescription}}
            Edges: {{edgesDescription}}

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
