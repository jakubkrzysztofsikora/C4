using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Subscriptions;

namespace C4.Modules.Discovery.Api.Persistence;

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
}
