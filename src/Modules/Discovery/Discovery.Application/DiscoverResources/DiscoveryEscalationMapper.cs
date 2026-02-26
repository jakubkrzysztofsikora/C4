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
                DiscoveryEscalationLevel.RetrySilently,
                "Discovery connector is temporarily unavailable. Retry in a few moments."),
            "discovery.auth.permission" => new(
                DiscoveryExecutionStatus.Failed,
                DiscoveryEscalationLevel.NotifyUser,
                "Reconnect the subscription or refresh credentials/permissions before retrying discovery."),
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
            UnauthorizedAccessException => DiscoveryErrors.AuthPermissionFailure("azure-resource-graph"),
            HttpRequestException => DiscoveryErrors.ConnectorUnavailable("azure-resource-graph"),
            System.Text.Json.JsonException => DiscoveryErrors.SchemaContractViolation("azure-resource-graph"),
            FormatException => DiscoveryErrors.SchemaContractViolation("azure-resource-graph"),
            InvalidDataException => DiscoveryErrors.SchemaContractViolation("azure-resource-graph"),
            _ => DiscoveryErrors.ConnectorUnavailable("azure-resource-graph")
        };
}
