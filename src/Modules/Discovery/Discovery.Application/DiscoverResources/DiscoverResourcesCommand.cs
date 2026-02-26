using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed record DiscoverResourcesCommand(
    Guid SubscriptionId,
    string ExternalSubscriptionId,
    Guid ProjectId,
    string? OrganizationId = null,
    IReadOnlyCollection<DiscoverySourceKind>? Sources = null)
    : IRequest<Result<DiscoverResourcesResponse>>;

public sealed record DiscoverResourcesResponse(Guid SubscriptionId, int ResourcesCount, DiscoveryPlan Plan);
