using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class AzureSubscriptionRepository(DiscoveryDbContext dbContext) : IAzureSubscriptionRepository
{
    public async Task<bool> ExistsByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken) =>
        await dbContext.Subscriptions.AnyAsync(s => s.ExternalSubscriptionId == externalSubscriptionId, cancellationToken);

    public async Task AddAsync(AzureSubscription subscription, CancellationToken cancellationToken) =>
        await dbContext.Subscriptions.AddAsync(subscription, cancellationToken);

    public Task<AzureSubscription?> GetFirstAsync(CancellationToken cancellationToken) =>
        dbContext.Subscriptions.FirstOrDefaultAsync(cancellationToken);

    public Task DeleteAsync(AzureSubscription subscription, CancellationToken cancellationToken)
    {
        dbContext.Subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }
}
