using C4.Modules.Discovery.Application.DiscoverResources;

namespace C4.Modules.Discovery.Tests.DiscoverResources;

public sealed class MultiSourceDiscoveryPlannerTests
{
    [Fact]
    public void BuildPlan_AzureUnavailable_ChoosesMcpTool()
    {
        var planner = new MultiSourceDiscoveryPlanner();

        var plan = planner.BuildPlan(azureAvailable: false, azureResultCount: 0, mcpResultCount: 2);

        plan.ChosenTools.Should().Contain("mcp-discovery");
        plan.RetriesOrFallbackPaths.Should().Contain("fallback:azure-resource-graph=>mcp-discovery");
    }

    [Fact]
    public void BuildPlan_PartialSuccess_DoesNotEscalate()
    {
        var planner = new MultiSourceDiscoveryPlanner();

        var plan = planner.BuildPlan(azureAvailable: true, azureResultCount: 3, mcpResultCount: 0);

        plan.FinalEscalationDecision.Should().Be(DiscoveryEscalationDecision.NoEscalation);
        plan.RetriesOrFallbackPaths.Should().Contain("partial-success:continue-with-available-source");
    }

    [Fact]
    public void BuildPlan_NormalizationContract_ContainsProvenanceForAllSources()
    {
        var planner = new MultiSourceDiscoveryPlanner();

        var plan = planner.BuildPlan(azureAvailable: true, azureResultCount: 5, mcpResultCount: 1);

        plan.NormalizationContract.Version.Should().Be("v1");
        plan.NormalizationContract.Provenance.Should().ContainEquivalentOf(new DiscoveryProvenanceItem("azure-resource-graph", 5));
        plan.NormalizationContract.Provenance.Should().ContainEquivalentOf(new DiscoveryProvenanceItem("mcp-discovery", 1));
    }
}
