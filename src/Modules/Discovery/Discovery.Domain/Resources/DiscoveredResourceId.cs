using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Resources;

public sealed record DiscoveredResourceId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static DiscoveredResourceId New() => new(Guid.NewGuid());
}
