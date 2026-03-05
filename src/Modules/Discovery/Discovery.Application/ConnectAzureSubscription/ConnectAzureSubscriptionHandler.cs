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
        var normalizedExternalId = request.ExternalSubscriptionId.Trim();

        if (await repository.ExistsByExternalIdAsync(normalizedExternalId, cancellationToken))
        {
            return Result<ConnectAzureSubscriptionResponse>.Failure(DiscoveryErrors.DuplicateSubscription(normalizedExternalId));
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

        try
        {
            await repository.AddAsync(subscriptionResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDuplicateSubscriptionPersistenceError(ex))
        {
            return Result<ConnectAzureSubscriptionResponse>.Failure(DiscoveryErrors.DuplicateSubscription(normalizedExternalId));
        }

        return Result<ConnectAzureSubscriptionResponse>.Success(new ConnectAzureSubscriptionResponse(
            subscriptionResult.Value.Id.Value,
            subscriptionResult.Value.ExternalSubscriptionId,
            subscriptionResult.Value.DisplayName));
    }

    private static bool IsDuplicateSubscriptionPersistenceError(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            var message = current.Message;
            if (!string.IsNullOrWhiteSpace(message)
                && (message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                && (message.Contains("ExternalSubscriptionId", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("external_subscription_id", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("azure_subscriptions", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("IX_azure_subscriptions_ExternalSubscriptionId", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
