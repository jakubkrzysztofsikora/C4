using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Subscriptions;

public sealed record AzureSubscriptionId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static AzureSubscriptionId New() => new(Guid.NewGuid());
}
