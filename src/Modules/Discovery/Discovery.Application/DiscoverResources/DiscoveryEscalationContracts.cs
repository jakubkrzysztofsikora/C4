namespace C4.Modules.Discovery.Application.DiscoverResources;

public enum DiscoveryExecutionStatus
{
    Success,
    Partial,
    Failed
}

public enum DiscoveryEscalationLevel
{
    RetrySilently,
    NotifyUser,
    BlockPipeline
}

public sealed record DiscoveryEscalationMetadata(
    DiscoveryExecutionStatus Status,
    DiscoveryEscalationLevel EscalationLevel,
    string? UserActionHint);
