using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoverResourcesHandler(
    IAzureResourceGraphClient resourceGraphClient,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IResourceClassifier classifier,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<DiscoverResourcesCommand, Result<DiscoverResourcesResponse>>
{
    public async Task<Result<DiscoverResourcesResponse>> Handle(DiscoverResourcesCommand request, CancellationToken cancellationToken)
    {
        var records = await resourceGraphClient.GetResourcesAsync(request.ExternalSubscriptionId, cancellationToken);

        var resources = new List<DiscoveredResource>();
        foreach (var record in records)
        {
            var classification = await classifier.ClassifyAsync(record.ResourceType, record.Name, cancellationToken);
            resources.Add(DiscoveredResource.Create(record.ResourceId, record.ResourceType, record.Name, classification));
        }

        await discoveredResourceRepository.UpsertRangeAsync(request.SubscriptionId, resources, cancellationToken);

        var diagramItems = resources
            .Where(r => r.Classification?.IncludeInDiagram ?? true)
            .Select(r => new DiscoveredResourceEventItem(
                r.ResourceId,
                r.ResourceType,
                r.Name,
                r.Classification?.FriendlyName,
                r.Classification?.ServiceType,
                r.Classification?.C4Level,
                r.Classification?.IncludeInDiagram ?? true))
            .ToArray();

        await mediator.Publish(new ResourcesDiscoveredIntegrationEvent(request.ProjectId, diagramItems), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DiscoverResourcesResponse>.Success(new DiscoverResourcesResponse(request.SubscriptionId, resources.Count));
    }
}
