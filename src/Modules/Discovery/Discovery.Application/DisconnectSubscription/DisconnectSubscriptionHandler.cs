using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.DisconnectSubscription;

public sealed class DisconnectSubscriptionHandler(
    IAzureSubscriptionRepository repository,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork
) : IRequestHandler<DisconnectSubscriptionCommand, Result<DisconnectSubscriptionResponse>>
{
    public async Task<Result<DisconnectSubscriptionResponse>> Handle(DisconnectSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetFirstAsync(cancellationToken);
        if (subscription is null)
        {
            return Result<DisconnectSubscriptionResponse>.Success(new DisconnectSubscriptionResponse(Guid.Empty));
        }

        var subscriptionId = subscription.Id.Value;
        await repository.DeleteAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DisconnectSubscriptionResponse>.Success(new DisconnectSubscriptionResponse(subscriptionId));
    }
}
