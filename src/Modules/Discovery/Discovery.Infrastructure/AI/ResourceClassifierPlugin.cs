using System.Text;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class ResourceClassifierPlugin(Kernel kernel, ILearningProvider? learningProvider = null, ILogger<ResourceClassifierPlugin>? logger = null) : IResourceClassifier
{
    public async Task<AzureResourceClassification> ClassifyAsync(Guid projectId, string armResourceType, string resourceName, CancellationToken cancellationToken)
    {
        var catalogResult = AzureResourceTypeCatalog.Classify(armResourceType);
        if (IsKnownType(armResourceType))
            return catalogResult;

        var learningsSection = await BuildLearningsSectionAsync(projectId, cancellationToken);

        var prompt = $"""
            Classify the following Azure resource for an architecture diagram.
            Resource Type: {armResourceType}
            Resource Name: {resourceName}
            {learningsSection}
            Respond in this exact format:
            FRIENDLY_NAME: <short friendly name>
            SERVICE_TYPE: <one of: app, api, database, queue, cache, external>
            C4_LEVEL: <one of: Context, Container, Component>
            INCLUDE: <true or false - true for workload resources, false for infrastructure plumbing>
            """;

        try
        {
            var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var text = result.GetValue<string>() ?? string.Empty;
            return ParseClassification(text, armResourceType) ?? catalogResult;
        }
        catch
        {
            return catalogResult;
        }
    }

    private async Task<string> BuildLearningsSectionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (learningProvider is null)
            return string.Empty;

        try
        {
            var learnings = await learningProvider.GetActiveLearningsAsync(projectId, "ResourceClassification", cancellationToken);
            if (learnings.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Previous user feedback learnings about resource classification:");
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

    private static bool IsKnownType(string armResourceType)
    {
        var defaultFallbackName = armResourceType.Split('/').Last();
        var catalogResult = AzureResourceTypeCatalog.Classify(armResourceType);
        return !string.Equals(catalogResult.FriendlyName, defaultFallbackName, StringComparison.Ordinal)
               || catalogResult.ServiceType != "external"
               || !catalogResult.IncludeInDiagram
               || catalogResult.C4Level != "Container";
    }

    private static AzureResourceClassification? ParseClassification(string text, string armResourceType)
    {
        string? friendlyName = null;
        string? serviceType = null;
        string? c4Level = null;
        bool include = true;

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("FRIENDLY_NAME:", StringComparison.OrdinalIgnoreCase))
                friendlyName = trimmed["FRIENDLY_NAME:".Length..].Trim();
            else if (trimmed.StartsWith("SERVICE_TYPE:", StringComparison.OrdinalIgnoreCase))
                serviceType = trimmed["SERVICE_TYPE:".Length..].Trim().ToLowerInvariant();
            else if (trimmed.StartsWith("C4_LEVEL:", StringComparison.OrdinalIgnoreCase))
                c4Level = trimmed["C4_LEVEL:".Length..].Trim();
            else if (trimmed.StartsWith("INCLUDE:", StringComparison.OrdinalIgnoreCase))
                include = !trimmed["INCLUDE:".Length..].Trim().Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        if (string.IsNullOrWhiteSpace(friendlyName) || string.IsNullOrWhiteSpace(serviceType) || string.IsNullOrWhiteSpace(c4Level))
            return null;

        var validServiceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "app", "api", "database", "queue", "cache", "external" };
        var validC4Levels = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Context", "Container", "Component" };

        if (!validServiceTypes.Contains(serviceType) || !validC4Levels.Contains(c4Level))
            return null;

        return new AzureResourceClassification(friendlyName, serviceType, c4Level, include);
    }
}
