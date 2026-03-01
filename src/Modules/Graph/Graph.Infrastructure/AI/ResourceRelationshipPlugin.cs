using System.Text;
using C4.Modules.Graph.Application.Ports;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace C4.Modules.Graph.Infrastructure.AI;

public sealed class ResourceRelationshipPlugin(Kernel kernel, ILearningProvider? learningProvider = null, ILogger<ResourceRelationshipPlugin>? logger = null) : IResourceRelationshipInferrer
{
    private const double MinimumConfidenceThreshold = 0.6;
    private volatile bool _aiAvailable = true;

    public async Task<IReadOnlyCollection<InferredRelationship>> InferRelationshipsAsync(
        Guid projectId,
        IReadOnlyCollection<ResourceForInference> resources,
        IReadOnlyCollection<string> existingEdgeDescriptions,
        CancellationToken cancellationToken)
    {
        if (!_aiAvailable)
            return [];

        var learningsSection = await BuildLearningsSectionAsync(projectId, cancellationToken);
        var allRelationships = new List<InferredRelationship>();

        var byResourceGroup = resources
            .GroupBy(r => r.ResourceGroup, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in byResourceGroup)
        {
            try
            {
                var prompt = BuildPrompt(group.Key, group.ToArray(), existingEdgeDescriptions, learningsSection);
                var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                var text = result.GetValue<string>() ?? string.Empty;
                allRelationships.AddRange(ParseRelationships(text));
            }
            catch (HttpRequestException)
            {
                logger?.LogWarning("AI backend unreachable; skipping relationship inference for remaining resource groups");
                _aiAvailable = false;
                break;
            }
        }

        return allRelationships
            .Where(r => r.Confidence >= MinimumConfidenceThreshold)
            .ToArray();
    }

    private static string BuildPrompt(
        string rgName,
        ResourceForInference[] groupResources,
        IReadOnlyCollection<string> existingEdgeDescriptions,
        string learningsSection)
    {
        var resourceLines = new StringBuilder();
        foreach (var r in groupResources)
            resourceLines.AppendLine($"- {r.ExternalResourceId}: {r.Name} (type: {r.ServiceType}, level: {r.C4Level})");

        var edgeLines = new StringBuilder();
        foreach (var edge in existingEdgeDescriptions)
            edgeLines.AppendLine($"- {edge}");

        return $"""
            Analyze the following Azure cloud resources and identify which services likely communicate with each other.

            Resources in resource group "{rgName}":
            {resourceLines}
            Already known connections:
            {edgeLines}
            {learningsSection}
            For each additional relationship you identify, respond with one line per relationship in this exact format:
            SOURCE_ID: <resource id>
            TARGET_ID: <resource id>
            CONFIDENCE: <0.0 to 1.0>

            Only suggest relationships where services would realistically communicate. Common patterns:
            - App services connect to databases, caches, and storage
            - Front Door / CDN / App Gateway routes to app services
            - App Insights monitors app services
            - Functions connect to queues and storage
            """;
    }

    private async Task<string> BuildLearningsSectionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (learningProvider is null)
            return string.Empty;

        try
        {
            var learnings = await learningProvider.GetActiveLearningsAsync(projectId, "RelationshipInference", cancellationToken);
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

    private static IEnumerable<InferredRelationship> ParseRelationships(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? sourceId = null;
        string? targetId = null;
        double? confidence = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("SOURCE_ID:", StringComparison.OrdinalIgnoreCase))
            {
                sourceId = trimmed["SOURCE_ID:".Length..].Trim();
            }
            else if (trimmed.StartsWith("TARGET_ID:", StringComparison.OrdinalIgnoreCase))
            {
                targetId = trimmed["TARGET_ID:".Length..].Trim();
            }
            else if (trimmed.StartsWith("CONFIDENCE:", StringComparison.OrdinalIgnoreCase))
            {
                var raw = trimmed["CONFIDENCE:".Length..].Trim();
                if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    confidence = parsed;
            }

            if (sourceId is not null && targetId is not null && confidence is not null)
            {
                yield return new InferredRelationship(sourceId, targetId, confidence.Value);
                sourceId = null;
                targetId = null;
                confidence = null;
            }
        }
    }
}
