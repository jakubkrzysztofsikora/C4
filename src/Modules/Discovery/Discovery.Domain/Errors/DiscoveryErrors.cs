using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Errors;

public static class DiscoveryErrors
{
    private static string? NormalizeDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var trimmed = detail.Trim();
        if (trimmed.Length <= 300)
            return trimmed;

        return $"{trimmed[..300]}...";
    }

    public static Error DuplicateSubscription(string externalId) =>
        new("discovery.subscription.duplicate", $"Subscription '{externalId}' is already connected.");

    public static Error InvalidSubscription(string externalId) =>
        new("discovery.subscription.invalid", $"Subscription '{externalId}' is invalid.");

    public static Error ConnectorUnavailable(string connectorName, string? detail = null) =>
        new(
            "discovery.connector.unavailable",
            NormalizeDetail(detail) is { } normalized
                ? $"Connector '{connectorName}' is unavailable: {normalized}"
                : $"Connector '{connectorName}' is unavailable.");

    public static Error AuthPermissionFailure(string providerName, string? detail = null) =>
        new(
            "discovery.auth.permission",
            NormalizeDetail(detail) is { } normalized
                ? $"Authorization failed for provider '{providerName}': {normalized}"
                : $"Authorization failed for provider '{providerName}'. Reconnect Azure and retry discovery.");

    public static Error SchemaContractViolation(string providerName) =>
        new("discovery.schema.contract", $"Provider '{providerName}' returned a schema/contract violating payload.");

    public static Error PartialDataQualityFailure(int failedResources) =>
        new("discovery.data-quality.partial", $"{failedResources} resources could not be processed due to data quality issues.");

    public static Error SubscriptionNotFound() =>
        new("discovery.subscription.not_found", "No subscription has been connected.");
}
