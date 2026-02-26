using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.IntegrationEventHandlers;

public sealed class ResourcesDiscoveredHandler(IArchitectureGraphRepository repository, IUnitOfWork unitOfWork)
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
}
