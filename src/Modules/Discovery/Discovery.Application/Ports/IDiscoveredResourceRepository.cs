using C4.Modules.Discovery.Domain.Resources;

namespace C4.Modules.Discovery.Application.Ports;

public interface IDiscoveredResourceRepository
{
    Task UpsertRangeAsync(Guid subscriptionId, IReadOnlyCollection<DiscoveredResource> resources, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DiscoveredResource>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);
}
