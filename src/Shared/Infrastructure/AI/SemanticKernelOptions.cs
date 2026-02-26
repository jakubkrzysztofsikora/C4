namespace C4.Shared.Infrastructure.AI;

public sealed class SemanticKernelOptions
{
    public const string SectionName = "SemanticKernel";

    public OllamaOptions Ollama { get; init; } = new();
    public List<RemoteMcpServerOptions> RemoteMcpServers { get; init; } = [];
    public Dictionary<string, ToolFilterOptions> ToolFiltersByEnvironment { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public PlannerExecutionLimitsOptions PlannerExecutionLimits { get; init; } = new();
}

public sealed class OllamaOptions
{
    public string Endpoint { get; init; } = "http://localhost:11434";
    public string ChatModel { get; init; } = "mistral-large-3:675b-cloud";
}

public sealed class RemoteMcpServerOptions
{
    public string Name { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string AuthMode { get; init; } = "None";
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class ToolFilterOptions
{
    public List<string> AllowList { get; init; } = [];
    public List<string> DenyList { get; init; } = [];
}

public sealed class PlannerExecutionLimitsOptions
{
    public int MaxSteps { get; init; } = 10;
    public int MaxRetries { get; init; } = 2;
    public decimal Budget { get; init; } = 1.0m;
}
