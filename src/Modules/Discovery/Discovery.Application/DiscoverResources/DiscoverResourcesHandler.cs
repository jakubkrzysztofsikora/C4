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
    IDiscoveryDataPreparer discoveryDataPreparer,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<DiscoverResourcesCommand, Result<DiscoverResourcesResponse>>
{
    public async Task<Result<DiscoverResourcesResponse>> Handle(DiscoverResourcesCommand request, CancellationToken cancellationToken)
    {
        var records = await resourceGraphClient.GetResourcesAsync(request.ExternalSubscriptionId, cancellationToken);
        var preparedRecords = discoveryDataPreparer.Prepare(records
            .Select(record => new RawDiscoveryRecord(
                record.ResourceId,
                record.ResourceType,
                record.Name,
                "azure",
                record.ParentResourceId))
            .ToArray());

        var classifiedPairs = new List<(PreparedDiscoveryRecord Record, DiscoveredResource Resource)>();
        foreach (var record in preparedRecords)
        {
            var classification = await classifier.ClassifyAsync(record.ResourceType, record.Name, cancellationToken);
            var resource = DiscoveredResource.Create(record.StableResourceId, record.ResourceType, record.Name, classification);
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
                p.Record.RawParentResourceId,
                p.Record.SourceProvenance,
                p.Record.ConfidenceScore,
                p.Record.Relationships.FirstOrDefault()?.RelationshipType,
                p.Record.Relationships.FirstOrDefault()?.RelatedStableResourceId,
                p.Record.StableResourceId))
            .ToArray();

        await mediator.Publish(new ResourcesDiscoveredIntegrationEvent(request.ProjectId, diagramItems), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DiscoverResourcesResponse>.Success(new DiscoverResourcesResponse(request.SubscriptionId, resources.Count));
    }
}
