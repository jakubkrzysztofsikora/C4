using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.ArchitectureGraph;

public sealed record ArchitectureGraphId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static ArchitectureGraphId New() => new(Guid.NewGuid());
}
