namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class MultiSourceDiscoveryPlanner
{
    public MultiSourceDiscoveryPlan BuildPlan(bool azureAvailable, int azureResultCount, int mcpResultCount)
    {
        var chosenTools = new List<string>();
        var steps = new List<string>();
        var retriesOrFallbacks = new List<string>();

        if (azureAvailable)
        {
            chosenTools.Add("azure-resource-graph");
            steps.Add("Query Azure Resource Graph for subscription inventory");
        }
        else
        {
            chosenTools.Add("mcp-discovery");
            steps.Add("Azure unavailable; route inventory query to MCP discovery tool");
            retriesOrFallbacks.Add("fallback:azure-resource-graph=>mcp-discovery");
        }

        var combinedCount = azureResultCount + mcpResultCount;
        var escalation = !azureAvailable && combinedCount == 0
            ? DiscoveryEscalationDecision.Escalate
            : (combinedCount == 0 ? DiscoveryEscalationDecision.Retry : DiscoveryEscalationDecision.NoEscalation);

        if (combinedCount > 0 && (azureResultCount == 0 || mcpResultCount == 0))
            retriesOrFallbacks.Add("partial-success:continue-with-available-source");

        var provenance = new DiscoveryNormalizationContract(
            "v1",
            [
                new DiscoveryProvenanceItem("azure-resource-graph", azureResultCount),
                new DiscoveryProvenanceItem("mcp-discovery", mcpResultCount),
            ]);

        return new MultiSourceDiscoveryPlan(chosenTools, steps, retriesOrFallbacks, escalation, provenance);
    }
}

public sealed record MultiSourceDiscoveryPlan(
    IReadOnlyCollection<string> ChosenTools,
    IReadOnlyCollection<string> PlanSteps,
    IReadOnlyCollection<string> RetriesOrFallbackPaths,
    DiscoveryEscalationDecision FinalEscalationDecision,
    DiscoveryNormalizationContract NormalizationContract);

public sealed record DiscoveryNormalizationContract(string Version, IReadOnlyCollection<DiscoveryProvenanceItem> Provenance);

public sealed record DiscoveryProvenanceItem(string Source, int ItemCount);

public enum DiscoveryEscalationDecision
{
    NoEscalation,
    Retry,
    Escalate,
}

