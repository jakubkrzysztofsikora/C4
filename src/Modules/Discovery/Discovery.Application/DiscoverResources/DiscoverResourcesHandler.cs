using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoverResourcesHandler(
    IDiscoveryInputProvider discoveryInputProvider,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IResourceClassifier classifier,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<DiscoverResourcesCommand, Result<DiscoverResourcesResponse>>
{
    public async Task<Result<DiscoverResourcesResponse>> Handle(DiscoverResourcesCommand request, CancellationToken cancellationToken)
    {
        var normalizedRequest = new NormalizedDiscoveryRequest(
            request.ProjectId,
            request.OrganizationId,
            request.ExternalSubscriptionId,
            request.Sources ?? DiscoverySourceKindDefaults.All);

        IReadOnlyCollection<DiscoveryResourceDescriptor> records;
        try
        {
            records = await discoveryInputProvider.GetResourcesAsync(normalizedRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            var error = DiscoveryEscalationMapper.MapExternalFailure(ex);
            return Result<DiscoverResourcesResponse>.Failure(error);
        }

        var classifiedPairs = new List<(DiscoveryResourceDescriptor Record, DiscoveredResource Resource)>();
        var dataQualityFailures = 0;
        foreach (var record in records)
        {
            try
            {
                var classification = await classifier.ClassifyAsync(record.ResourceType, record.Name, cancellationToken);
                var resource = DiscoveredResource.Create(record.ResourceId, record.ResourceType, record.Name, classification);
                classifiedPairs.Add((record, resource));
            }
            catch
            {
                dataQualityFailures++;
            }
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

        var escalation = dataQualityFailures > 0
            ? DiscoveryEscalationMapper.ForPartialDataQuality()
            : DiscoveryEscalationMapper.ForSuccess();

        return Result<DiscoverResourcesResponse>.Success(
            new DiscoverResourcesResponse(
                request.SubscriptionId,
                resources.Count,
                escalation.Status,
                escalation.EscalationLevel,
                escalation.UserActionHint,
                dataQualityFailures));
    }
}
