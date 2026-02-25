using C4.Modules.Graph.Domain.GraphSnapshot;
using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.Events;

public sealed record GraphUpdatedEvent(Guid ProjectId, GraphSnapshotId SnapshotId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
