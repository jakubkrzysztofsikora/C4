using C4.Modules.Discovery.Domain.Errors;
using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public static class DiscoveryEscalationMapper
{
    public static DiscoveryEscalationMetadata ForSuccess() =>
        new(DiscoveryExecutionStatus.Success, DiscoveryEscalationLevel.RetrySilently, null);

    public static DiscoveryEscalationMetadata ForPartialDataQuality() =>
        new(
            DiscoveryExecutionStatus.Partial,
            DiscoveryEscalationLevel.NotifyUser,
            "Some resources could not be interpreted. You can continue with a partial diagram and review adapter logs.");

    public static DiscoveryEscalationMetadata ForFailure(Error error) =>
        error.Code switch
        {
            "discovery.connector.unavailable" => new(
                DiscoveryExecutionStatus.Failed,
                DiscoveryEscalationLevel.NotifyUser,
                "Discovery connector is temporarily unavailable. Retry shortly and check provider connectivity."),
            "discovery.auth.permission" => new(
                DiscoveryExecutionStatus.Failed,
                DiscoveryEscalationLevel.NotifyUser,
                "Azure authorization expired or is invalid. Reconnect your Azure subscription, then retry discovery."),
            "discovery.schema.contract" => new(
                DiscoveryExecutionStatus.Failed,
                DiscoveryEscalationLevel.BlockPipeline,
                "Stop automatic processing and contact support. Adapter contract mismatch must be fixed."),
            "discovery.data-quality.partial" => ForPartialDataQuality(),
            _ => new(
                DiscoveryExecutionStatus.Failed,
                DiscoveryEscalationLevel.NotifyUser,
                "Discovery failed unexpectedly. Review logs and retry.")
        };

    public static Error MapExternalFailure(Exception exception) =>
        exception switch
        {
            UnauthorizedAccessException ex => DiscoveryErrors.AuthPermissionFailure("azure-resource-graph", ex.Message),
            InvalidOperationException ex when IsAuthRelated(ex.Message) => DiscoveryErrors.AuthPermissionFailure("azure-resource-graph", ex.Message),
            HttpRequestException ex => DiscoveryErrors.ConnectorUnavailable("azure-resource-graph", ex.Message),
            System.Text.Json.JsonException => DiscoveryErrors.SchemaContractViolation("azure-resource-graph"),
            FormatException => DiscoveryErrors.SchemaContractViolation("azure-resource-graph"),
            InvalidDataException => DiscoveryErrors.SchemaContractViolation("azure-resource-graph"),
            _ => DiscoveryErrors.ConnectorUnavailable("azure-resource-graph", exception.Message)
        };

    private static bool IsAuthRelated(string message) =>
        message.Contains("credentials", StringComparison.OrdinalIgnoreCase)
        || message.Contains("re-authenticate", StringComparison.OrdinalIgnoreCase)
        || message.Contains("token expired", StringComparison.OrdinalIgnoreCase)
        || message.Contains("token refresh", StringComparison.OrdinalIgnoreCase);
}
