using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Subscriptions;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class InMemoryAzureSubscriptionRepository : IAzureSubscriptionRepository
{
    private readonly List<AzureSubscription> _subscriptions = [];

    public Task<bool> ExistsByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken)
        => Task.FromResult(_subscriptions.Any(subscription => subscription.ExternalSubscriptionId == externalSubscriptionId));

    public Task AddAsync(AzureSubscription subscription, CancellationToken cancellationToken)
    {
        _subscriptions.Add(subscription);
        return Task.CompletedTask;
    }

    public Task<AzureSubscription?> GetFirstAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_subscriptions.FirstOrDefault());

    public Task DeleteAsync(AzureSubscription subscription, CancellationToken cancellationToken)
    {
        _subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }
}
