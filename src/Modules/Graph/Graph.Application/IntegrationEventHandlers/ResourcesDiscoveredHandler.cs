using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
using C4.Modules.Graph.Domain;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;

namespace C4.Modules.Graph.Application.IntegrationEventHandlers;

public sealed class ResourcesDiscoveredHandler(
    IArchitectureGraphRepository repository,
    [FromKeyedServices("Graph")] IUnitOfWork unitOfWork,
    IResourceRelationshipInferrer? relationshipInferrer = null,
    IMediator? mediator = null)
    : INotificationHandler<ResourcesDiscoveredIntegrationEvent>
{
    private const int ExternalResourceIdMaxLength = 500;
    private const int NameMaxLength = 250;
    private const int ServiceTypeMaxLength = 200;
    private const int DomainMaxLength = 200;
    private const int ClassificationSourceMaxLength = 50;
    private static readonly TimeSpan AiInferenceTimeout = TimeSpan.FromSeconds(20);
    private const int MaxAiInferenceResourceGroups = 10;

    public async Task Handle(ResourcesDiscoveredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var graph = await repository.GetByProjectIdAsync(notification.ProjectId, cancellationToken)
            ?? Domain.ArchitectureGraph.ArchitectureGraph.Create(notification.ProjectId);

        var includedResources = notification.Resources
            .GroupBy(r => r.StableResourceId ?? r.ResourceId)
            .Select(group => group
                .OrderByDescending(r => r.ConfidenceScore)
                .ThenBy(r => GetSourcePriority(r.SourceProvenance))
                .First())
            .ToArray();

        foreach (var resource in includedResources)
        {
            var effectiveClassification = ResolveClassification(resource);
            var level = ParseC4Level(effectiveClassification.C4Level);
            var resourceName = SanitizeDisplayName(resource.Name);
            var friendlyName = SanitizeDisplayName(resource.FriendlyName);
            var displayName = !string.IsNullOrWhiteSpace(friendlyName)
                ? $"{resourceName} ({friendlyName})"
                : resourceName;
            var nodeExternalResourceId = NormalizeIdentifier(resource.StableResourceId ?? resource.ResourceId, ExternalResourceIdMaxLength);
            graph.AddOrUpdateNode(
                nodeExternalResourceId,
                Truncate(displayName, NameMaxLength),
                level,
                Truncate(effectiveClassification.ServiceType, ServiceTypeMaxLength, "external"),
                domain: Truncate(resource.Domain, DomainMaxLength, "General"),
                isInfrastructure: effectiveClassification.IsInfrastructure,
                classificationSource: Truncate(effectiveClassification.ClassificationSource, ClassificationSourceMaxLength, "fallback"),
                classificationConfidence: effectiveClassification.ClassificationConfidence,
                tags: ExtractTags(resource.Tags));
        }

        var parentMappings = includedResources
            .Where(r => r.ParentResourceId is not null)
            .Select(r => new
            {
                Child = NormalizeIdentifier(r.StableResourceId ?? r.ResourceId, ExternalResourceIdMaxLength),
                Parent = NormalizeIdentifier(r.ParentResourceId!, ExternalResourceIdMaxLength),
                Score = r.ConfidenceScore
            })
            .GroupBy(item => item.Child, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.Score).First())
            .ToDictionary(
                item => item.Child,
                item => item.Parent,
                StringComparer.OrdinalIgnoreCase);

        graph.ResolveNodeParents(parentMappings);

        InferEdges(graph, includedResources);

        if (relationshipInferrer is not null)
        {
            try
            {
                await InferRelationshipsWithAiAsync(graph, includedResources, relationshipInferrer, notification.ProjectId, cancellationToken);
            }
            catch (Exception)
            {
            }
        }

        graph.CreateSnapshot();
        await repository.UpsertAsync(graph, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (mediator is not null)
        {
            await mediator.Publish(
                new GraphChangedIntegrationEvent(notification.ProjectId, "discovery", DateTime.UtcNow),
                cancellationToken);
        }
    }

    private static EffectiveClassification ResolveClassification(DiscoveredResourceEventItem resource)
    {
        var catalog = AzureResourceTypeCatalog.Classify(resource.ResourceType);
        bool hasValidLevel = Enum.TryParse<C4Level>(resource.C4Level, ignoreCase: true, out _);
        bool hasServiceType = !string.IsNullOrWhiteSpace(resource.ServiceType);
        if (hasValidLevel && hasServiceType)
        {
            var isInfrastructure = resource.IsInfrastructure || catalog.IsInfrastructure;
            var classificationSource = string.Equals(resource.ClassificationSource, "fallback", StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(catalog.ClassificationSource, "catalog", StringComparison.OrdinalIgnoreCase)
                ? "catalog"
                : resource.ClassificationSource;
            var classificationConfidence = Math.Max(resource.ClassificationConfidence, catalog.Confidence);
            return new EffectiveClassification(
                resource.ServiceType!,
                resource.C4Level!,
                isInfrastructure,
                classificationSource,
                classificationConfidence);
        }

        return new EffectiveClassification(
            catalog.ServiceType,
            catalog.C4Level,
            catalog.IsInfrastructure,
            catalog.ClassificationSource,
            catalog.Confidence);
    }

    private static async Task InferRelationshipsWithAiAsync(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        DiscoveredResourceEventItem[] includedResources,
        IResourceRelationshipInferrer inferrer,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var orphanNodeIds = graph.Nodes
            .Where(n => !graph.Edges.Any(e => e.SourceNodeId == n.Id || e.TargetNodeId == n.Id))
            .Select(n => n.ExternalResourceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groupedOrphans = includedResources
            .Where(r => orphanNodeIds.Contains(r.StableResourceId ?? r.ResourceId))
            .Select(r => new
            {
                Resource = r,
                ResourceGroup = ExtractResourceGroup(r.StableResourceId ?? r.ResourceId)
            })
            .Where(x => x.ResourceGroup is not null)
            .GroupBy(x => x.ResourceGroup!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Count())
            .Take(MaxAiInferenceResourceGroups)
            .ToArray();

        if (groupedOrphans.Length == 0)
            return;

        var resourcesForInference = groupedOrphans
            .SelectMany(g => g.Select(x => x.Resource))
            .Distinct()
            .Select(r => new ResourceForInference(
                r.StableResourceId ?? r.ResourceId,
                r.Name,
                r.ServiceType ?? string.Empty,
                r.C4Level ?? string.Empty,
                ExtractResourceGroup(r.StableResourceId ?? r.ResourceId) ?? string.Empty))
            .ToArray();

        if (resourcesForInference.Length == 0)
            return;

        var existingEdgeDescriptions = graph.Edges
            .Select(e =>
            {
                var source = graph.Nodes.FirstOrDefault(n => n.Id == e.SourceNodeId);
                var target = graph.Nodes.FirstOrDefault(n => n.Id == e.TargetNodeId);
                return source is not null && target is not null
                    ? $"{source.ExternalResourceId} -> {target.ExternalResourceId}"
                    : null;
            })
            .OfType<string>()
            .ToArray();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(AiInferenceTimeout);
        var inferred = await inferrer.InferRelationshipsAsync(projectId, resourcesForInference, existingEdgeDescriptions, timeoutCts.Token);

        foreach (var relationship in inferred.Where(r => r.Confidence >= 0.6))
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n =>
                n.ExternalResourceId.Equals(NormalizeIdentifier(relationship.SourceResourceId, ExternalResourceIdMaxLength), StringComparison.OrdinalIgnoreCase));
            var targetNode = graph.Nodes.FirstOrDefault(n =>
                n.ExternalResourceId.Equals(NormalizeIdentifier(relationship.TargetResourceId, ExternalResourceIdMaxLength), StringComparison.OrdinalIgnoreCase));

            if (sourceNode is not null && targetNode is not null)
                graph.AddEdge(sourceNode, targetNode);
        }
    }

    private static C4Level ParseC4Level(string? c4LevelValue)
    {
        if (Enum.TryParse<C4Level>(c4LevelValue, ignoreCase: true, out var parsed))
            return parsed;

        return C4Level.Container;
    }

    private static IReadOnlyCollection<string> ExtractTags(IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is null || tags.Count == 0)
            return [];

        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in tags)
        {
            if (!string.IsNullOrWhiteSpace(key))
                values.Add(key.Trim());

            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value.Trim());

            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                values.Add($"{key.Trim()}:{value.Trim()}");
        }

        return values.Count == 0 ? [] : values.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int GetSourcePriority(string sourceProvenance)
    {
        var normalized = sourceProvenance.Trim().ToLowerInvariant();
        if (normalized == "azure")
            return 0;

        if (normalized == "repo")
            return 1;

        if (normalized.StartsWith("mcp:"))
            return 2;

        return 3;
    }

    private static void InferEdges(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        DiscoveredResourceEventItem[] includedResources)
    {
        InferPropertyReferenceEdges(graph, includedResources);
        InferParentChildEdges(graph, includedResources);
        InferResourceGroupHeuristicEdges(graph, includedResources);
    }

    private static void InferPropertyReferenceEdges(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        DiscoveredResourceEventItem[] includedResources)
    {
        foreach (var resource in includedResources.Where(r => r.Relationships is { Count: > 0 }))
        {
            var sourceId = resource.StableResourceId ?? resource.ResourceId;
            var sourceNode = graph.Nodes.FirstOrDefault(n =>
                n.ExternalResourceId.Equals(NormalizeIdentifier(sourceId, ExternalResourceIdMaxLength), StringComparison.OrdinalIgnoreCase));
            if (sourceNode is null) continue;

            foreach (var rel in resource.Relationships!)
            {
                var targetNode = graph.Nodes.FirstOrDefault(n =>
                    n.ExternalResourceId.Equals(NormalizeIdentifier(rel.TargetResourceId, ExternalResourceIdMaxLength), StringComparison.OrdinalIgnoreCase));
                if (targetNode is not null)
                    graph.AddEdge(sourceNode, targetNode);
            }
        }
    }

    private static void InferParentChildEdges(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        DiscoveredResourceEventItem[] includedResources)
    {
        foreach (var resource in includedResources.Where(r => r.ParentResourceId is not null))
        {
            var childId = resource.StableResourceId ?? resource.ResourceId;
            var childNode = graph.Nodes.FirstOrDefault(n =>
                n.ExternalResourceId.Equals(NormalizeIdentifier(childId, ExternalResourceIdMaxLength), StringComparison.OrdinalIgnoreCase));
            var parentNode = graph.Nodes.FirstOrDefault(n =>
                n.ExternalResourceId.Equals(NormalizeIdentifier(resource.ParentResourceId!, ExternalResourceIdMaxLength), StringComparison.OrdinalIgnoreCase));
            if (childNode is not null && parentNode is not null)
                graph.AddEdge(parentNode, childNode);
        }
    }

    private static void InferResourceGroupHeuristicEdges(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        DiscoveredResourceEventItem[] includedResources)
    {
        var producerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "app", "api", "external" };
        var consumerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "database", "queue", "cache", "storage" };
        var monitoringTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "monitoring" };
        var appTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "app", "api" };

        var byResourceGroup = includedResources
            .Select(r => new { Resource = r, ResourceGroup = ExtractResourceGroup(r.StableResourceId ?? r.ResourceId) })
            .Where(x => x.ResourceGroup is not null)
            .GroupBy(x => x.ResourceGroup!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in byResourceGroup)
        {
            var workloadResources = group.Where(x => !x.Resource.IsInfrastructure).ToArray();
            if (workloadResources.Length == 0)
                continue;

            var producers = workloadResources.Where(x => producerTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var consumers = workloadResources.Where(x => consumerTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var monitors = workloadResources.Where(x => monitoringTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var apps = workloadResources.Where(x => appTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();

            foreach (var producer in producers)
            {
                foreach (var consumer in consumers)
                {
                    var sourceNode = FindNode(graph, producer.Resource);
                    var targetNode = FindNode(graph, consumer.Resource);
                    if (sourceNode is not null && targetNode is not null)
                        graph.AddEdge(sourceNode, targetNode);
                }
            }

            foreach (var monitor in monitors)
            {
                foreach (var app in apps)
                {
                    var monitorNode = FindNode(graph, monitor.Resource);
                    var appNode = FindNode(graph, app.Resource);
                    if (monitorNode is not null && appNode is not null)
                        graph.AddEdge(monitorNode, appNode);
                }
            }
        }

        InferCrossResourceGroupNetworkingEdges(graph, includedResources);
    }

    private static void InferCrossResourceGroupNetworkingEdges(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        DiscoveredResourceEventItem[] includedResources)
    {
        var networkingTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "external" };
        var backendTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "app", "api" };

        var networkingResources = includedResources
            .Where(r => networkingTypes.Contains(r.ServiceType ?? "") && !r.IsInfrastructure)
            .ToArray();
        var backendResources = includedResources
            .Where(r => backendTypes.Contains(r.ServiceType ?? "") && !r.IsInfrastructure)
            .ToArray();

        if (networkingResources.Length == 0 || backendResources.Length == 0) return;

        foreach (var networking in networkingResources)
        {
            var networkingPrefix = ExtractNamePrefix(networking.Name);
            if (networkingPrefix is null) continue;

            foreach (var backend in backendResources)
            {
                var backendPrefix = ExtractNamePrefix(backend.Name);
                if (backendPrefix is null) continue;

                if (!networkingPrefix.Equals(backendPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                var sourceNode = FindNode(graph, networking);
                var targetNode = FindNode(graph, backend);
                if (sourceNode is not null && targetNode is not null)
                    graph.AddEdge(sourceNode, targetNode);
            }
        }
    }

    private static Domain.GraphNode.GraphNode? FindNode(
        Domain.ArchitectureGraph.ArchitectureGraph graph,
        DiscoveredResourceEventItem resource)
    {
        var id = resource.StableResourceId ?? resource.ResourceId;
        var normalizedId = NormalizeIdentifier(id, ExternalResourceIdMaxLength);
        return graph.Nodes.FirstOrDefault(n => n.ExternalResourceId.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractNamePrefix(string name)
    {
        var parts = name.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        return string.Join("-", parts.Take(parts.Length > 3 ? 3 : 2)).ToLowerInvariant();
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

    private sealed record EffectiveClassification(
        string ServiceType,
        string C4Level,
        bool IsInfrastructure,
        string ClassificationSource,
        double ClassificationConfidence);

    private static string Truncate(string? value, int maxLength, string fallback = "")
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (trimmed.Length <= maxLength)
            return trimmed;

        return trimmed[..maxLength];
    }

    private static string NormalizeIdentifier(string value, int maxLength)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
            return trimmed;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trimmed)))[..16].ToLowerInvariant();
        const string separator = "~";
        var prefixLength = Math.Max(1, maxLength - hash.Length - separator.Length);
        return $"{trimmed[..prefixLength]}{separator}{hash}";
    }

    private static string SanitizeDisplayName(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.EndsWith(" ()", StringComparison.Ordinal))
            return trimmed[..^3].TrimEnd();
        return trimmed;
    }
}
