using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.GetDiscoveryStatus;

public sealed class GetDiscoveryStatusHandler(IDiscoveredResourceRepository discoveredResourceRepository, IDriftResultRepository driftResultRepository)
    : IRequestHandler<GetDiscoveryStatusQuery, Result<GetDiscoveryStatusResponse>>
{
    public async Task<Result<GetDiscoveryStatusResponse>> Handle(GetDiscoveryStatusQuery request, CancellationToken cancellationToken)
    {
        var discovered = await discoveredResourceRepository.GetBySubscriptionAsync(request.SubscriptionId, cancellationToken);
        var drift = await driftResultRepository.GetBySubscriptionAsync(request.SubscriptionId, cancellationToken);

        return Result<GetDiscoveryStatusResponse>.Success(new GetDiscoveryStatusResponse(
            request.SubscriptionId,
            discovered.Count,
            drift.Count(item => item.Status == "Drifted"),
            DateTime.UtcNow));
    }
}
