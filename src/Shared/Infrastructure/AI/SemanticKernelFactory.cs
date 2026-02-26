using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace C4.Shared.Infrastructure.AI;

public sealed record SemanticKernelCreationResult(
    Microsoft.SemanticKernel.Kernel Kernel,
    IReadOnlyCollection<string> EnabledTools);

public interface ISemanticKernelFactory
{
    SemanticKernelCreationResult Create(string moduleName, IEnumerable<string> discoveredTools);
}

public interface ISemanticKernelTelemetryHook
{
    void OnKernelCreated(SemanticKernelBuildContext context);
}

public sealed record SemanticKernelBuildContext(
    string ModuleName,
    string Environment,
    IReadOnlyCollection<string> DiscoveredTools,
    IReadOnlyCollection<string> EnabledTools,
    IReadOnlyCollection<RemoteMcpServerOptions> RemoteMcpServers,
    PlannerExecutionLimitsOptions PlannerExecutionLimits);

internal sealed class SemanticKernelFactory(
    IOptions<SemanticKernelOptions> options,
    IHostEnvironment hostEnvironment,
    IEnumerable<ISemanticKernelTelemetryHook> telemetryHooks,
    ILogger<SemanticKernelFactory> logger) : ISemanticKernelFactory
{
    private readonly SemanticKernelOptions _options = options.Value;

    public SemanticKernelCreationResult Create(string moduleName, IEnumerable<string> discoveredTools)
    {
        var toolNames = discoveredTools.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var filterOptions = ResolveToolFilterOptions(hostEnvironment.EnvironmentName);
        var enabledTools = toolNames
            .Where(tool => IsToolEnabled(tool, filterOptions))
            .ToArray();

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
        builder.AddOllamaChatCompletion(_options.Ollama.ChatModel, new Uri(_options.Ollama.Endpoint));
#pragma warning restore SKEXP0070

        var kernel = builder.Build();

        var context = new SemanticKernelBuildContext(
            moduleName,
            hostEnvironment.EnvironmentName,
            toolNames,
            enabledTools,
            _options.RemoteMcpServers,
            _options.PlannerExecutionLimits);

        foreach (var telemetryHook in telemetryHooks)
        {
            telemetryHook.OnKernelCreated(context);
        }

        logger.LogInformation(
            "Created Semantic Kernel for module {ModuleName} in {Environment}. Tools discovered: {DiscoveredCount}; enabled after filters: {EnabledCount}; remote MCP servers: {McpCount}.",
            moduleName,
            hostEnvironment.EnvironmentName,
            toolNames.Length,
            enabledTools.Length,
            _options.RemoteMcpServers.Count);

        return new SemanticKernelCreationResult(kernel, enabledTools);
    }

    private ToolFilterOptions ResolveToolFilterOptions(string environmentName)
    {
        return _options.ToolFiltersByEnvironment.TryGetValue(environmentName, out var environmentOptions)
            ? environmentOptions
            : new ToolFilterOptions();
    }

    private static bool IsToolEnabled(string toolName, ToolFilterOptions options)
    {
        if (options.DenyList.Contains(toolName))
        {
            return false;
        }

        if (options.AllowList.Count == 0)
        {
            return true;
        }

        return options.AllowList.Contains(toolName);
    }
}

internal sealed class LoggingSemanticKernelTelemetryHook(ILogger<LoggingSemanticKernelTelemetryHook> logger)
    : ISemanticKernelTelemetryHook
{
    public void OnKernelCreated(SemanticKernelBuildContext context)
    {
        logger.LogDebug(
            "Semantic Kernel telemetry: module={Module}; environment={Environment}; planner(maxSteps={MaxSteps}, retries={Retries}, budget={Budget}); enabled tools=[{Tools}]",
            context.ModuleName,
            context.Environment,
            context.PlannerExecutionLimits.MaxSteps,
            context.PlannerExecutionLimits.MaxRetries,
            context.PlannerExecutionLimits.Budget,
            string.Join(", ", context.EnabledTools));
    }
}
