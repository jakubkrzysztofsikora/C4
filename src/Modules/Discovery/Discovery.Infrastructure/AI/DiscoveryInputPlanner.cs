using System.Text.Json;
using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class DiscoveryInputPlanner : IDiscoveryInputPlanner
{
    private const string PlannerPluginName = "discovery_tools";

    private const string PlannerPrompt = """
        <system>
        You are a discovery orchestration planner.
        Build a practical execution plan from user intent and input context.
        Always call available plugin functions before finalizing the plan.
        Return JSON only.
        Rules:
        - Build an ordered task list.
        - Every task must include dependencies and at least one fallback route.
        - Use discovered tools only.
        - Prefer Azure connector for runtime inventory, repo parsers for IaC context, and MCP tools for external enrichment.
        JSON schema:
        {
          "tasks": [
            {
              "taskId": "t1",
              "order": 1,
              "toolName": "azure.resource_graph",
              "action": "...",
              "dependsOnTaskIds": [],
              "fallbackToolNames": ["..."]
            }
          ]
        }
        </system>

        <userIntent>{{$intent}}</userIntent>
        <inputContext>{{$context}}</inputContext>
        """;

    private readonly Kernel _kernel;

    public DiscoveryInputPlanner(Kernel kernel)
    {
        if (!kernel.Plugins.Contains(PlannerPluginName))
            kernel.Plugins.AddFromObject(new DiscoveryPlannerToolsPlugin(), PlannerPluginName);
        _kernel = kernel;
    }

    public async Task<DiscoveryPlan> BuildPlanAsync(string userIntent, string inputContext, CancellationToken cancellationToken)
    {
        var arguments = new KernelArguments(new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
        {
            ["intent"] = userIntent,
            ["context"] = inputContext
        };

        var result = await _kernel.InvokePromptAsync(PlannerPrompt, arguments, cancellationToken: cancellationToken);
        var text = result.GetValue<string>() ?? string.Empty;

        var parsed = TryParsePlan(text);
        return parsed is null
            ? CreateFallbackPlan(userIntent, inputContext)
            : parsed with { UserIntent = userIntent, InputContext = inputContext };
    }

    private static DiscoveryPlan? TryParsePlan(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            if (!document.RootElement.TryGetProperty("tasks", out var tasksElement) || tasksElement.ValueKind != JsonValueKind.Array)
                return null;

            var tasks = new List<PlannedToolInvocation>();
            foreach (var element in tasksElement.EnumerateArray())
            {
                if (!element.TryGetProperty("taskId", out var taskIdElement) ||
                    !element.TryGetProperty("order", out var orderElement) ||
                    !element.TryGetProperty("toolName", out var toolNameElement) ||
                    !element.TryGetProperty("action", out var actionElement) ||
                    !orderElement.TryGetInt32(out var order))
                {
                    continue;
                }

                var taskId = taskIdElement.GetString() ?? string.Empty;
                var toolName = toolNameElement.GetString() ?? string.Empty;
                var action = actionElement.GetString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(toolName) || string.IsNullOrWhiteSpace(action))
                    continue;

                var dependencies = element.TryGetProperty("dependsOnTaskIds", out var deps)
                    ? deps.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                    : [];
                var fallbacks = element.TryGetProperty("fallbackToolNames", out var fallback)
                    ? fallback.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                    : [];

                tasks.Add(new PlannedToolInvocation(taskId, order, toolName, action, dependencies, fallbacks));
            }

            return tasks.Count == 0
                ? null
                : new DiscoveryPlan(string.Empty, string.Empty, tasks.OrderBy(t => t.Order).ToArray());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DiscoveryPlan CreateFallbackPlan(string userIntent, string inputContext)
    {
        var tasks = new[]
        {
            new PlannedToolInvocation("t1", 1, DiscoveryToolNames.AzureResourceGraph, "Collect live Azure resources for the target subscription", [], [DiscoveryToolNames.RepoBicepParser, DiscoveryToolNames.RepoTerraformParser]),
            new PlannedToolInvocation("t2", 2, DiscoveryToolNames.RepoBicepParser, "Parse repository Bicep templates to enrich architecture intent", ["t1"], [DiscoveryToolNames.RepoTerraformParser]),
            new PlannedToolInvocation("t3", 3, DiscoveryToolNames.McpRemoteDiscovery, "Enrich model with remote MCP tools for supplemental metadata", ["t1"], [DiscoveryToolNames.AzureResourceGraph])
        };

        return new DiscoveryPlan(userIntent, inputContext, tasks);
    }
}
