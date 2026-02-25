using C4.Modules.Discovery.Contracts.IntegrationEvents;
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

        foreach (var resource in notification.Resources)
        {
            graph.AddOrUpdateNode(resource.ResourceId, resource.Name, MapLevel(resource.ResourceType));
        }

        graph.CreateSnapshot();
        await repository.UpsertAsync(graph, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static C4Level MapLevel(string type) => type.Contains("function", StringComparison.OrdinalIgnoreCase) ? C4Level.Component : C4Level.Container;
}
