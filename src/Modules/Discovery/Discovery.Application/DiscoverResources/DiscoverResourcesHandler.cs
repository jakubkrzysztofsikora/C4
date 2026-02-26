using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoverResourcesHandler(
    IDiscoveryInputPlanner planner,
    IAzureResourceGraphClient resourceGraphClient,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IResourceClassifier classifier,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<DiscoverResourcesCommand, Result<DiscoverResourcesResponse>>
{
    public async Task<Result<DiscoverResourcesResponse>> Handle(DiscoverResourcesCommand request, CancellationToken cancellationToken)
    {
        var plan = await planner.BuildPlanAsync(
            "Discover Azure resources for connected subscription",
            $"SubscriptionId={request.SubscriptionId}; ExternalSubscriptionId={request.ExternalSubscriptionId}; ProjectId={request.ProjectId}",
            cancellationToken);

        var records = await resourceGraphClient.GetResourcesAsync(request.ExternalSubscriptionId, cancellationToken);

        var classifiedPairs = new List<(AzureResourceRecord Record, DiscoveredResource Resource)>();
        foreach (var record in records)
        {
            var classification = await classifier.ClassifyAsync(record.ResourceType, record.Name, cancellationToken);
            var resource = DiscoveredResource.Create(record.ResourceId, record.ResourceType, record.Name, classification);
            classifiedPairs.Add((record, resource));
        }

        var resources = classifiedPairs.Select(p => p.Resource).ToList();
        await discoveredResourceRepository.UpsertRangeAsync(request.SubscriptionId, resources, cancellationToken);

        var diagramItems = classifiedPairs
            .Where(p => p.Resource.Classification?.IncludeInDiagram ?? true)
            .Select(p => new DiscoveredResourceEventItem(
                p.Resource.ResourceId,
                p.Resource.ResourceType,
                p.Resource.Name,
                p.Resource.Classification?.FriendlyName,
                p.Resource.Classification?.ServiceType,
                p.Resource.Classification?.C4Level,
                p.Resource.Classification?.IncludeInDiagram ?? true,
                p.Record.ParentResourceId))
            .ToArray();

        await mediator.Publish(new ResourcesDiscoveredIntegrationEvent(request.ProjectId, diagramItems), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DiscoverResourcesResponse>.Success(new DiscoverResourcesResponse(request.SubscriptionId, resources.Count, plan));
    }
}
