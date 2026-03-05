using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Shared.Kernel.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Infrastructure.Services;

public sealed class DriftQueryService(
    DiscoveryDbContext dbContext,
    ILogger<DriftQueryService> logger) : IDriftQueryService
{
    public async Task<IReadOnlyCollection<string>> GetDriftedResourceIdsAsync(
        IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
    {
        if (resourceIds.Count == 0)
            return [];

        try
        {
            return await dbContext.DriftItems
                .Where(d => resourceIds.Contains(d.ResourceId) && d.Status == "Drifted")
                .Select(d => d.ResourceId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read drift items for requested resource scope");
            return [];
        }
    }
}
