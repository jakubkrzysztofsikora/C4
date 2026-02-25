using C4.Modules.Discovery.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class DriftResultRepository(DiscoveryDbContext dbContext) : IDriftResultRepository
{
    public async Task SaveAsync(Guid subscriptionId, IReadOnlyCollection<DriftItem> driftItems, CancellationToken cancellationToken)
    {
        var existing = await dbContext.DriftItems
            .Where(d => d.SubscriptionId == subscriptionId)
            .ToListAsync(cancellationToken);

        dbContext.DriftItems.RemoveRange(existing);

        foreach (var item in driftItems)
        {
            await dbContext.DriftItems.AddAsync(new DriftItemEntity
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                ResourceId = item.ResourceId,
                Status = item.Status
            }, cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<DriftItem>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken) =>
        await dbContext.DriftItems
            .Where(d => d.SubscriptionId == subscriptionId)
            .Select(d => new DriftItem(d.ResourceId, d.Status))
            .ToListAsync(cancellationToken);
}
