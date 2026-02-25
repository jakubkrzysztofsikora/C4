using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoverResourcesHandler(
    IAzureResourceGraphClient resourceGraphClient,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<DiscoverResourcesCommand, Result<DiscoverResourcesResponse>>
{
    public async Task<Result<DiscoverResourcesResponse>> Handle(DiscoverResourcesCommand request, CancellationToken cancellationToken)
    {
        var records = await resourceGraphClient.GetResourcesAsync(request.ExternalSubscriptionId, cancellationToken);
        var resources = records.Select(r => DiscoveredResource.Create(r.ResourceId, r.ResourceType, r.Name)).ToArray();

        await discoveredResourceRepository.UpsertRangeAsync(request.SubscriptionId, resources, cancellationToken);

        await mediator.Publish(new ResourcesDiscoveredIntegrationEvent(
            request.ProjectId,
            resources.Select(r => new DiscoveredResourceEventItem(r.ResourceId, r.ResourceType, r.Name)).ToArray()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DiscoverResourcesResponse>.Success(new DiscoverResourcesResponse(request.SubscriptionId, resources.Length));
    }
}
