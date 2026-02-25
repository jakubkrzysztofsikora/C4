using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Errors;
using C4.Modules.Discovery.Domain.Subscriptions;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ConnectAzureSubscription;

public sealed class ConnectAzureSubscriptionHandler(IAzureSubscriptionRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<ConnectAzureSubscriptionCommand, Result<ConnectAzureSubscriptionResponse>>
{
    public async Task<Result<ConnectAzureSubscriptionResponse>> Handle(ConnectAzureSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByExternalIdAsync(request.ExternalSubscriptionId.Trim(), cancellationToken))
        {
            return Result<ConnectAzureSubscriptionResponse>.Failure(DiscoveryErrors.DuplicateSubscription(request.ExternalSubscriptionId.Trim()));
        }

        var subscriptionResult = AzureSubscription.Connect(request.ExternalSubscriptionId, request.DisplayName);
        if (subscriptionResult.IsFailure)
        {
            return Result<ConnectAzureSubscriptionResponse>.Failure(subscriptionResult.Error);
        }

        await repository.AddAsync(subscriptionResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ConnectAzureSubscriptionResponse>.Success(new ConnectAzureSubscriptionResponse(
            subscriptionResult.Value.Id.Value,
            subscriptionResult.Value.ExternalSubscriptionId,
            subscriptionResult.Value.DisplayName));
    }
}
