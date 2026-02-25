using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Member;

public sealed record MemberId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static MemberId New() => new(Guid.NewGuid());
}
