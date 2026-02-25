using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.GetDiscoveryStatus;

public sealed record GetDiscoveryStatusQuery(Guid SubscriptionId) : IRequest<Result<GetDiscoveryStatusResponse>>;

public sealed record GetDiscoveryStatusResponse(Guid SubscriptionId, int DiscoveredResources, int DriftedResources, DateTime GeneratedAtUtc);
