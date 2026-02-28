using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Errors;

public static class DiscoveryErrors
{
    public static Error DuplicateSubscription(string externalId) =>
        new("discovery.subscription.duplicate", $"Subscription '{externalId}' is already connected.");

    public static Error InvalidSubscription(string externalId) =>
        new("discovery.subscription.invalid", $"Subscription '{externalId}' is invalid.");

    public static Error ConnectorUnavailable(string connectorName, string? detail = null) =>
        new("discovery.connector.unavailable", detail is not null
            ? $"Connector '{connectorName}' failed: {detail}"
            : $"Connector '{connectorName}' is unavailable.");

    public static Error AuthPermissionFailure(string providerName, string? detail = null) =>
        new("discovery.auth.permission", detail is not null
            ? $"Authorization failed for '{providerName}': {detail}"
            : $"Authorization failed for provider '{providerName}'.");

    public static Error SchemaContractViolation(string providerName) =>
        new("discovery.schema.contract", $"Provider '{providerName}' returned a schema/contract violating payload.");

    public static Error PartialDataQualityFailure(int failedResources) =>
        new("discovery.data-quality.partial", $"{failedResources} resources could not be processed due to data quality issues.");

    public static Error SubscriptionNotFound() =>
        new("discovery.subscription.not_found", "No subscription has been connected.");
}
