using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Errors;

public static class DiscoveryErrors
{
    public static Error DuplicateSubscription(string externalId) =>
        new("discovery.subscription.duplicate", $"Subscription '{externalId}' is already connected.");

    public static Error InvalidSubscription(string externalId) =>
        new("discovery.subscription.invalid", $"Subscription '{externalId}' is invalid.");
}
