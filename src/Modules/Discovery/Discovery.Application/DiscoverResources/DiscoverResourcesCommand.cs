using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed record DiscoverResourcesCommand(Guid SubscriptionId, string ExternalSubscriptionId, Guid ProjectId)
    : IRequest<Result<DiscoverResourcesResponse>>;

public sealed record DiscoverResourcesResponse(Guid SubscriptionId, int ResourcesCount);
