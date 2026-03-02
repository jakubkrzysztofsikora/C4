using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Errors;
using C4.Modules.Discovery.Domain.Subscriptions;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Security;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.ConnectAzureSubscription;

public sealed class ConnectAzureSubscriptionHandler(
    IAzureSubscriptionRepository repository,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork,
    IDataProtectionService dataProtectionService)
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

        var encryptedPat = string.IsNullOrWhiteSpace(request.GitPatToken)
            ? null
            : dataProtectionService.Protect(request.GitPatToken.Trim());

        subscriptionResult.Value.ConfigureGitRepository(request.GitRepoUrl, encryptedPat);

        await repository.AddAsync(subscriptionResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ConnectAzureSubscriptionResponse>.Success(new ConnectAzureSubscriptionResponse(
            subscriptionResult.Value.Id.Value,
            subscriptionResult.Value.ExternalSubscriptionId,
            subscriptionResult.Value.DisplayName));
    }
}
