using System.Text;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class ResourceClassifierPlugin(
    Kernel kernel,
    ILearningProvider? learningProvider = null,
    IProjectArchitectureContextProvider? architectureContextProvider = null,
    ILogger<ResourceClassifierPlugin>? logger = null) : IResourceClassifier
{
    private volatile bool _aiAvailable = true;

    public async Task<AzureResourceClassification> ClassifyAsync(Guid projectId, string armResourceType, string resourceName, CancellationToken cancellationToken)
    {
        var catalogResult = AzureResourceTypeCatalog.Classify(armResourceType);
        if (IsKnownType(armResourceType))
            return catalogResult;

        if (!_aiAvailable)
            return catalogResult;

        var contextSection = await BuildArchitectureContextSectionAsync(projectId, cancellationToken);
        var learningsSection = await BuildLearningsSectionAsync(projectId, cancellationToken);

        var prompt = $"""
            Classify the following Azure resource for an architecture diagram.
            Resource Type: {armResourceType}
            Resource Name: {resourceName}
            {contextSection}
            {learningsSection}
            Respond in this exact format:
            FRIENDLY_NAME: <short friendly name>
            SERVICE_TYPE: <one of: app, api, database, queue, cache, storage, monitoring, external, boundary>
            C4_LEVEL: <one of: Context, Container, Component, Code>
            INCLUDE: <true or false - true for workload resources, false for infrastructure plumbing>
            """;

        try
        {
            var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var text = result.GetValue<string>() ?? string.Empty;
            return ParseClassification(text, armResourceType) ?? catalogResult;
        }
        catch (HttpRequestException)
        {
            logger?.LogWarning("AI backend unreachable; disabling AI classification for remaining resources");
            _aiAvailable = false;
            return catalogResult;
        }
        catch
        {
            return catalogResult;
        }
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

    private static bool IsKnownType(string armResourceType) =>
        AzureResourceTypeCatalog.IsKnown(armResourceType);

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

        var validServiceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "app", "api", "database", "queue", "cache", "storage", "monitoring", "external", "boundary" };
        var validC4Levels = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Context", "Container", "Component", "Code" };

        if (!validServiceTypes.Contains(serviceType) || !validC4Levels.Contains(c4Level))
            return null;

        bool isInfrastructure = InferInfrastructure(armResourceType, serviceType, c4Level, include);
        return new AzureResourceClassification(
            friendlyName,
            serviceType,
            c4Level,
            include,
            ClassificationSource: "ai",
            Confidence: 0.82,
            IsInfrastructure: isInfrastructure);
    }

    private static bool InferInfrastructure(string armResourceType, string serviceType, string c4Level, bool include)
    {
        if (!include)
            return true;

        if (serviceType.Equals("boundary", StringComparison.OrdinalIgnoreCase))
            return false;

        if (serviceType.Equals("external", StringComparison.OrdinalIgnoreCase)
            && c4Level.Equals("Component", StringComparison.OrdinalIgnoreCase))
            return true;

        return armResourceType.Contains("/network", StringComparison.OrdinalIgnoreCase)
            && c4Level.Equals("Component", StringComparison.OrdinalIgnoreCase);
    }
}
