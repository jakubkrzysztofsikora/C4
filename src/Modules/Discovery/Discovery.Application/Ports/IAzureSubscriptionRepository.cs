using C4.Modules.Discovery.Domain.Subscriptions;

namespace C4.Modules.Discovery.Application.Ports;

public interface IAzureSubscriptionRepository
{
    Task<bool> ExistsByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken);

    Task AddAsync(AzureSubscription subscription, CancellationToken cancellationToken);
}
