using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.User;

public sealed record UserId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static UserId New() => new(Guid.NewGuid());
}
