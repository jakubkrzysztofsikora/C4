using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class DiscoveredResourceRepository(
    DiscoveryDbContext dbContext,
    ILogger<DiscoveredResourceRepository> logger) : IDiscoveredResourceRepository
{
    public async Task UpsertRangeAsync(Guid subscriptionId, IReadOnlyCollection<DiscoveredResource> resources, CancellationToken cancellationToken)
    {
        var dedupedResources = resources
            .Where(resource => !string.IsNullOrWhiteSpace(resource.ResourceId))
            .GroupBy(resource => resource.ResourceId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        var removedDuplicates = resources.Count - dedupedResources.Length;
        if (removedDuplicates > 0)
        {
            logger.LogWarning(
                "Discovery persistence dedupe removed {RemovedDuplicates} duplicate resources for subscription {SubscriptionId}",
                removedDuplicates,
                subscriptionId);
        }

        await DeleteExistingForSubscriptionAsync(subscriptionId, cancellationToken);

        foreach (var resource in dedupedResources)
        {
            dbContext.Entry(resource).Property("SubscriptionId").CurrentValue = subscriptionId;
            await dbContext.Resources.AddAsync(resource, cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<DiscoveredResource>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken) =>
        await dbContext.Resources
            .Where(r => EF.Property<Guid>(r, "SubscriptionId") == subscriptionId)
            .ToListAsync(cancellationToken);

    private async Task DeleteExistingForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Resources
                .Where(r => EF.Property<Guid>(r, "SubscriptionId") == subscriptionId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            var existing = await dbContext.Resources
                .Where(r => EF.Property<Guid>(r, "SubscriptionId") == subscriptionId)
                .ToListAsync(cancellationToken);
            dbContext.Resources.RemoveRange(existing);
        }
    }
}
