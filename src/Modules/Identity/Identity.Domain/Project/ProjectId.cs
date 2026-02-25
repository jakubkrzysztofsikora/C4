using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Project;

public sealed record ProjectId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static ProjectId New() => new(Guid.NewGuid());
}
