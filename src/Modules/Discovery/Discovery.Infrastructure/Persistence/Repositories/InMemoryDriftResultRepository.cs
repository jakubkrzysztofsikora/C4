using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class InMemoryDriftResultRepository : IDriftResultRepository
{
    private readonly Dictionary<Guid, List<DriftItem>> _store = [];

    public Task SaveAsync(Guid subscriptionId, IReadOnlyCollection<DriftItem> driftItems, CancellationToken cancellationToken)
    {
        _store[subscriptionId] = driftItems.ToList();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DriftItem>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(subscriptionId, out var items);
        return Task.FromResult<IReadOnlyCollection<DriftItem>>(items ?? []);
    }
}
