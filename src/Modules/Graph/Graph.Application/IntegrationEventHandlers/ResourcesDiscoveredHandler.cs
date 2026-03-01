using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Graph.Application.IntegrationEventHandlers;

public sealed class ResourcesDiscoveredHandler(
    IArchitectureGraphRepository repository,
    [FromKeyedServices("Graph")] IUnitOfWork unitOfWork,
    IResourceRelationshipInferrer? relationshipInferrer = null)
    : INotificationHandler<ResourcesDiscoveredIntegrationEvent>
{
    public async Task Handle(ResourcesDiscoveredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var graph = await repository.GetByProjectIdAsync(notification.ProjectId, cancellationToken)
            ?? Domain.ArchitectureGraph.ArchitectureGraph.Create(notification.ProjectId);

        var includedResources = notification.Resources
            .Where(r => r.IncludeInDiagram)
            .GroupBy(r => r.StableResourceId ?? r.ResourceId)
            .Select(group => group
                .OrderByDescending(r => r.ConfidenceScore)
                .ThenBy(r => GetSourcePriority(r.SourceProvenance))
                .First())
            .ToArray();

        foreach (var resource in includedResources)
        {
            var level = ParseC4Level(resource.C4Level);
            var displayName = resource.FriendlyName is not null
                ? $"{resource.Name} ({resource.FriendlyName})"
                : resource.Name;
            graph.AddOrUpdateNode(resource.StableResourceId ?? resource.ResourceId, displayName, level);
        }

        var parentMappings = includedResources
            .Where(r => r.ParentResourceId is not null)
            .ToDictionary(
                r => r.StableResourceId ?? r.ResourceId,
                r => r.ParentResourceId!);

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

        var orphanResources = includedResources
            .Where(r => orphanNodeIds.Contains(r.StableResourceId ?? r.ResourceId))
            .Select(r => new
            {
                Resource = r,
                ResourceGroup = ExtractResourceGroup(r.StableResourceId ?? r.ResourceId)
            })
            .Where(x => x.ResourceGroup is not null)
            .GroupBy(x => x.ResourceGroup!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(x => x.Resource))
            .Distinct()
            .ToArray();

        if (orphanResources.Length == 0)
            return;

        var resourcesForInference = orphanResources
            .Select(r => new ResourceForInference(
                r.StableResourceId ?? r.ResourceId,
                r.Name,
                r.ServiceType ?? string.Empty,
                r.C4Level ?? string.Empty,
                ExtractResourceGroup(r.StableResourceId ?? r.ResourceId) ?? string.Empty))
            .ToArray();

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

        var inferred = await inferrer.InferRelationshipsAsync(projectId, resourcesForInference, existingEdgeDescriptions, cancellationToken);

        foreach (var relationship in inferred.Where(r => r.Confidence >= 0.6))
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n =>
                n.ExternalResourceId.Equals(relationship.SourceResourceId, StringComparison.OrdinalIgnoreCase));
            var targetNode = graph.Nodes.FirstOrDefault(n =>
                n.ExternalResourceId.Equals(relationship.TargetResourceId, StringComparison.OrdinalIgnoreCase));

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
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == sourceId);
            if (sourceNode is null) continue;

            foreach (var rel in resource.Relationships!)
            {
                var targetNode = graph.Nodes.FirstOrDefault(n =>
                    n.ExternalResourceId.Equals(rel.TargetResourceId, StringComparison.OrdinalIgnoreCase));
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
            var childNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == childId);
            var parentNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == resource.ParentResourceId);
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
            var producers = group.Where(x => producerTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var consumers = group.Where(x => consumerTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var monitors = group.Where(x => monitoringTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var apps = group.Where(x => appTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();

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
            .Where(r => networkingTypes.Contains(r.ServiceType ?? ""))
            .ToArray();
        var backendResources = includedResources
            .Where(r => backendTypes.Contains(r.ServiceType ?? ""))
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
        return graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == id);
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
}
