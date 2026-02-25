using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.GraphSnapshot;

public sealed record GraphSnapshotId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static GraphSnapshotId New() => new(Guid.NewGuid());
}
