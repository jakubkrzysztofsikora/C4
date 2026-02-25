using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace C4.Host.Tests.Architecture;

[Trait("Category", "Architecture")]
public sealed class LayerDependencyTests
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

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\..*\.Domain").As("Domain Layer");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\..*\.Application").As("Application Layer");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInNamespaceMatching(@"C4\.Modules\..*\.Infrastructure").As("Infrastructure Layer");

    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .Check(Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .Check(Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .Check(Architecture);
    }
}
