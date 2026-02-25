using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.GraphNode;

public sealed record GraphNodeId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static GraphNodeId New() => new(Guid.NewGuid());
}
