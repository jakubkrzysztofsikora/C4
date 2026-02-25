using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class DiscoveredResourceRepository(DiscoveryDbContext dbContext) : IDiscoveredResourceRepository
{
    public async Task UpsertRangeAsync(Guid subscriptionId, IReadOnlyCollection<DiscoveredResource> resources, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Resources
            .Where(r => EF.Property<Guid>(r, "SubscriptionId") == subscriptionId)
            .ToListAsync(cancellationToken);

        dbContext.Resources.RemoveRange(existing);

        foreach (var resource in resources)
        {
            dbContext.Entry(resource).Property("SubscriptionId").CurrentValue = subscriptionId;
            await dbContext.Resources.AddAsync(resource, cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<DiscoveredResource>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken) =>
        await dbContext.Resources
            .Where(r => EF.Property<Guid>(r, "SubscriptionId") == subscriptionId)
            .ToListAsync(cancellationToken);
}
