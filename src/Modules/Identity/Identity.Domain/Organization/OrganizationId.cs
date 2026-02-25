using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Organization;

public sealed record OrganizationId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static OrganizationId New() => new(Guid.NewGuid());
}
