namespace C4.Modules.Discovery.Application.Ports;

public interface IDriftResultRepository
{
    Task SaveAsync(Guid subscriptionId, IReadOnlyCollection<DriftItem> driftItems, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DriftItem>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);
}

public sealed record DriftItem(string ResourceId, string Status);
