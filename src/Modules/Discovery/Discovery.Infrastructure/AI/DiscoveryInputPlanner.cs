using System.Text.Json;
using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class DiscoveryInputPlanner(Kernel kernel) : IDiscoveryInputPlanner
{
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

    public async Task<DiscoveryPlan> BuildPlanAsync(string userIntent, string inputContext, CancellationToken cancellationToken)
    {
        if (!kernel.Plugins.Any(plugin => string.Equals(plugin.Name, "discovery_tools", StringComparison.Ordinal)))
            kernel.Plugins.AddFromObject(new DiscoveryPlannerToolsPlugin(), "discovery_tools");

        var arguments = new KernelArguments(new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
        {
            ["intent"] = userIntent,
            ["context"] = inputContext
        };

        var result = await kernel.InvokePromptAsync(PlannerPrompt, arguments, cancellationToken: cancellationToken);
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
                var taskId = element.GetProperty("taskId").GetString() ?? string.Empty;
                var order = element.GetProperty("order").GetInt32();
                var toolName = element.GetProperty("toolName").GetString() ?? string.Empty;
                var action = element.GetProperty("action").GetString() ?? string.Empty;
                var dependencies = element.TryGetProperty("dependsOnTaskIds", out var deps)
                    ? deps.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                    : [];
                var fallbacks = element.TryGetProperty("fallbackToolNames", out var fallback)
                    ? fallback.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                    : [];

                if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(toolName) || string.IsNullOrWhiteSpace(action))
                    continue;

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
            new PlannedToolInvocation("t1", 1, "azure.resource_graph", "Collect live Azure resources for the target subscription", [], ["repo.bicep_parser", "repo.terraform_parser"]),
            new PlannedToolInvocation("t2", 2, "repo.bicep_parser", "Parse repository Bicep templates to enrich architecture intent", ["t1"], ["repo.terraform_parser"]),
            new PlannedToolInvocation("t3", 3, "mcp.remote_discovery", "Enrich model with remote MCP tools for supplemental metadata", ["t1"], ["azure.resource_graph"])
        };

        return new DiscoveryPlan(userIntent, inputContext, tasks);
    }
}
