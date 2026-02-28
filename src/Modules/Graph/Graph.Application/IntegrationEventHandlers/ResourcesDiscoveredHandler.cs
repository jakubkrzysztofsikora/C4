using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Graph.Application.IntegrationEventHandlers;

public sealed class ResourcesDiscoveredHandler(IArchitectureGraphRepository repository, [FromKeyedServices("Graph")] IUnitOfWork unitOfWork)
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
            .Where(r => r.NormalizedRelatedResourceId is not null || r.ParentResourceId is not null)
            .ToDictionary(
                r => r.StableResourceId ?? r.ResourceId,
                r => r.NormalizedRelatedResourceId ?? r.ParentResourceId!);

        graph.ResolveNodeParents(parentMappings);

        InferEdges(graph, includedResources);

        graph.CreateSnapshot();
        await repository.UpsertAsync(graph, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
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
        foreach (var resource in includedResources.Where(r => r.NormalizedRelatedResourceId is not null))
        {
            var sourceId = resource.StableResourceId ?? resource.ResourceId;
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == sourceId);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == resource.NormalizedRelatedResourceId);
            if (sourceNode is not null && targetNode is not null)
                graph.AddEdge(sourceNode, targetNode);
        }

        InferParentChildEdges(graph, includedResources);
        InferResourceGroupHeuristicEdges(graph, includedResources);
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
        var producerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "app", "api" };
        var consumerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "database", "queue", "cache", "storage", "monitoring" };

        var byResourceGroup = includedResources
            .Select(r => new { Resource = r, ResourceGroup = ExtractResourceGroup(r.StableResourceId ?? r.ResourceId) })
            .Where(x => x.ResourceGroup is not null)
            .GroupBy(x => x.ResourceGroup!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in byResourceGroup)
        {
            var producers = group.Where(x => producerTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var consumers = group.Where(x => consumerTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var externals = group.Where(x => string.Equals(x.Resource.ServiceType, "external", StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var producer in producers)
            {
                foreach (var consumer in consumers)
                {
                    var producerId = producer.Resource.StableResourceId ?? producer.Resource.ResourceId;
                    var consumerId = consumer.Resource.StableResourceId ?? consumer.Resource.ResourceId;
                    var sourceNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == producerId);
                    var targetNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == consumerId);
                    if (sourceNode is not null && targetNode is not null)
                        graph.AddEdge(sourceNode, targetNode);
                }

                foreach (var ext in externals)
                {
                    var producerId = producer.Resource.StableResourceId ?? producer.Resource.ResourceId;
                    var extId = ext.Resource.StableResourceId ?? ext.Resource.ResourceId;
                    var sourceNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == producerId);
                    var targetNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == extId);
                    if (sourceNode is not null && targetNode is not null)
                        graph.AddEdge(sourceNode, targetNode);
                }
            }
        }
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
