using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.GetSubscription;

public sealed class GetSubscriptionHandler(IAzureSubscriptionRepository repository)
    : IRequestHandler<GetSubscriptionQuery, Result<GetSubscriptionResponse>>
{
    public async Task<Result<GetSubscriptionResponse>> Handle(GetSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetFirstAsync(cancellationToken);
        if (subscription is null)
        {
            return Result<GetSubscriptionResponse>.Failure(DiscoveryErrors.SubscriptionNotFound());
        }

        return Result<GetSubscriptionResponse>.Success(
            new GetSubscriptionResponse(
                subscription.Id.Value,
                subscription.ExternalSubscriptionId,
                subscription.DisplayName,
                subscription.GitRepoUrl,
                subscription.GitBranch,
                subscription.GitRootPath,
                !string.IsNullOrWhiteSpace(subscription.GitPatToken)));
    }
}
