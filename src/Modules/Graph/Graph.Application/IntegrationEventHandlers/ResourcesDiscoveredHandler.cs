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

        var includedResources = notification.Resources.Where(r => r.IncludeInDiagram).ToArray();

        foreach (var resource in includedResources)
        {
            var level = ParseC4Level(resource.C4Level);
            var displayName = resource.FriendlyName ?? resource.Name;
            graph.AddOrUpdateNode(resource.ResourceId, displayName, level);
        }

        var parentMappings = includedResources
            .Where(r => r.ParentResourceId is not null)
            .ToDictionary(r => r.ResourceId, r => r.ParentResourceId!);

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
}
