namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed record DiscoveryPlan(
    string UserIntent,
    string InputContext,
    IReadOnlyCollection<PlannedToolInvocation> Tasks);

public sealed record PlannedToolInvocation(
    string TaskId,
    int Order,
    string ToolName,
    string Action,
    IReadOnlyCollection<string> DependsOnTaskIds,
    IReadOnlyCollection<string> FallbackToolNames);
