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
            var displayName = resource.FriendlyName ?? resource.Name;
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

        var appServiceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "app", "api" };
        var backendServiceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "database", "queue", "cache" };

        var byResourceGroup = includedResources
            .Select(r => new { Resource = r, ResourceGroup = ExtractResourceGroup(r.StableResourceId ?? r.ResourceId) })
            .Where(x => x.ResourceGroup is not null)
            .GroupBy(x => x.ResourceGroup!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in byResourceGroup)
        {
            var apps = group.Where(x => appServiceTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();
            var backends = group.Where(x => backendServiceTypes.Contains(x.Resource.ServiceType ?? "")).ToArray();

            foreach (var app in apps)
            {
                foreach (var backend in backends)
                {
                    var appId = app.Resource.StableResourceId ?? app.Resource.ResourceId;
                    var backendId = backend.Resource.StableResourceId ?? backend.Resource.ResourceId;
                    var sourceNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == appId);
                    var targetNode = graph.Nodes.FirstOrDefault(n => n.ExternalResourceId == backendId);
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
