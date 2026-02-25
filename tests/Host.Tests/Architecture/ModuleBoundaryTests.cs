using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace C4.Host.Tests.Architecture;

[Trait("Category", "Architecture")]
public sealed class ModuleBoundaryTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Modules.Identity.Domain.Organization.Organization).Assembly,
                typeof(Modules.Identity.Application.RegisterOrganization.RegisterOrganizationCommand).Assembly,
                typeof(Modules.Identity.Infrastructure.Persistence.IdentityDbContext).Assembly,
                typeof(Modules.Discovery.Domain.Subscriptions.AzureSubscription).Assembly,
                typeof(Modules.Discovery.Application.ConnectAzureSubscription.ConnectAzureSubscriptionCommand).Assembly,
                typeof(Modules.Discovery.Infrastructure.Persistence.DiscoveryDbContext).Assembly,
                typeof(Modules.Graph.Domain.ArchitectureGraph.ArchitectureGraph).Assembly,
                typeof(Modules.Graph.Application.GetGraph.GetGraphQuery).Assembly,
                typeof(Modules.Graph.Infrastructure.Persistence.GraphDbContext).Assembly,
                typeof(Modules.Telemetry.Domain.Metrics.ServiceHealth).Assembly,
                typeof(Modules.Telemetry.Application.IngestTelemetry.IngestTelemetryCommand).Assembly,
                typeof(Modules.Telemetry.Infrastructure.Persistence.TelemetryDbContext).Assembly,
                typeof(Modules.Visualization.Domain.Preset.ViewPreset).Assembly,
                typeof(Modules.Visualization.Application.GetDiagram.GetDiagramQuery).Assembly,
                typeof(Modules.Visualization.Infrastructure.Persistence.VisualizationDbContext).Assembly)
            .Build();

    [Fact]
    public void IdentityModule_ShouldNotDependOn_GraphModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Identity")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Graph"))
            .Check(Architecture);
    }

    [Fact]
    public void IdentityModule_ShouldNotDependOn_DiscoveryModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Identity")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Discovery"))
            .Check(Architecture);
    }

    [Fact]
    public void IdentityModule_ShouldNotDependOn_TelemetryModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Identity")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Telemetry"))
            .Check(Architecture);
    }

    [Fact]
    public void IdentityModule_ShouldNotDependOn_VisualizationModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Identity")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Visualization"))
            .Check(Architecture);
    }

    [Fact]
    public void TelemetryModule_ShouldNotDependOn_IdentityModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Telemetry")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Identity"))
            .Check(Architecture);
    }

    [Fact]
    public void VisualizationModule_ShouldNotDependOn_IdentityModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Visualization")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Identity"))
            .Check(Architecture);
    }

    [Fact]
    public void TelemetryModule_ShouldNotDependOn_GraphModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Telemetry")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Graph"))
            .Check(Architecture);
    }

    [Fact]
    public void VisualizationModule_ShouldNotDependOn_GraphModule()
    {
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Visualization")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"C4\.Modules\.Graph"))
            .Check(Architecture);
    }
}
