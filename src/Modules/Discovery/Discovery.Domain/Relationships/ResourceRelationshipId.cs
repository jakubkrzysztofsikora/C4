using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Relationships;

public sealed record ResourceRelationshipId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static ResourceRelationshipId New() => new(Guid.NewGuid());
}
