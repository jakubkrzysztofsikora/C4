using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class InMemoryDiscoveredResourceRepository : IDiscoveredResourceRepository
{
    private readonly Dictionary<Guid, List<DiscoveredResource>> _store = [];

    public Task UpsertRangeAsync(Guid subscriptionId, IReadOnlyCollection<DiscoveredResource> resources, CancellationToken cancellationToken)
    {
        _store[subscriptionId] = resources.ToList();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DiscoveredResource>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(subscriptionId, out var resources);
        return Task.FromResult<IReadOnlyCollection<DiscoveredResource>>(resources ?? []);
    }
}
