using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.GraphEdge;

public sealed record GraphEdgeId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static GraphEdgeId New() => new(Guid.NewGuid());
}
