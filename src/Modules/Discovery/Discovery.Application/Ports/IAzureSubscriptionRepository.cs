using C4.Modules.Discovery.Domain.Subscriptions;

namespace C4.Modules.Discovery.Application.Ports;

public interface IAzureSubscriptionRepository
{
    Task<bool> ExistsByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken);

    Task AddAsync(AzureSubscription subscription, CancellationToken cancellationToken);

    Task<AzureSubscription?> GetFirstAsync(CancellationToken cancellationToken);

    Task<AzureSubscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken);

    Task DeleteAsync(AzureSubscription subscription, CancellationToken cancellationToken);
}
