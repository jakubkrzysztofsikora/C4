using C4.Modules.Discovery.Domain.Resources;

namespace C4.Modules.Discovery.Tests.Domain;

public sealed class AzureResourceTypeCatalogTests
{
    [Fact]
    public void Classify_WebSites_ReturnsAppContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Web/sites");

        result.FriendlyName.Should().Be("App Service");
        result.ServiceType.Should().Be("app");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_PostgreSQL_ReturnsDatabaseContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.DBforPostgreSQL/flexibleServers");

        result.FriendlyName.Should().Be("PostgreSQL");
        result.ServiceType.Should().Be("database");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_FunctionApp_ReturnsAppComponent()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Web/sites/functions");

        result.FriendlyName.Should().Be("Function App");
        result.ServiceType.Should().Be("app");
        result.C4Level.Should().Be("Component");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_NetworkInterface_ExcludedFromDiagram()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Network/networkInterfaces");

        result.IncludeInDiagram.Should().BeFalse();
        result.FriendlyName.Should().Be("NIC");
    }

    [Fact]
    public void Classify_NSG_ExcludedFromDiagram()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Network/networkSecurityGroups");

        result.IncludeInDiagram.Should().BeFalse();
        result.FriendlyName.Should().Be("NSG");
    }

    [Fact]
    public void Classify_ManagedDisk_ExcludedFromDiagram()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Compute/disks");

        result.IncludeInDiagram.Should().BeFalse();
        result.FriendlyName.Should().Be("Disk");
    }

    [Fact]
    public void Classify_UnknownType_ReturnsDefaultContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.SomeUnknown/widgets");

        result.FriendlyName.Should().Be("widgets");
        result.ServiceType.Should().Be("external");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_CaseInsensitive_MatchesCorrectly()
    {
        var lower = AzureResourceTypeCatalog.Classify("microsoft.web/sites");
        var upper = AzureResourceTypeCatalog.Classify("MICROSOFT.WEB/SITES");
        var mixed = AzureResourceTypeCatalog.Classify("Microsoft.Web/sites");

        lower.FriendlyName.Should().Be("App Service");
        upper.FriendlyName.Should().Be("App Service");
        mixed.FriendlyName.Should().Be("App Service");
    }

    [Fact]
    public void Classify_Databricks_ReturnsDatabricksContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Databricks/workspaces");

        result.FriendlyName.Should().Be("Databricks");
        result.ServiceType.Should().Be("app");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_DataFactory_ReturnsDataFactoryContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.DataFactory/factories");

        result.FriendlyName.Should().Be("Data Factory");
        result.ServiceType.Should().Be("app");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_SynapseWorkspace_ReturnsSynapseContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Synapse/workspaces");

        result.FriendlyName.Should().Be("Synapse Analytics");
        result.ServiceType.Should().Be("app");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_StreamAnalytics_ReturnsStreamAnalyticsContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.StreamAnalytics/streamingjobs");

        result.FriendlyName.Should().Be("Stream Analytics");
        result.ServiceType.Should().Be("app");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_DataExplorer_ReturnsDataExplorerContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Kusto/clusters");

        result.FriendlyName.Should().Be("Data Explorer");
        result.ServiceType.Should().Be("database");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_ManagedHSM_ReturnsManagedHSMContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.KeyVault/managedHSMs");

        result.FriendlyName.Should().Be("Managed HSM");
        result.ServiceType.Should().Be("storage");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_AzureFirewall_ReturnsFirewallContainer()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Network/azureFirewalls");

        result.FriendlyName.Should().Be("Azure Firewall");
        result.ServiceType.Should().Be("external");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Classify_FirewallPolicy_ReturnsFirewallPolicyExcludedFromDiagram()
    {
        var result = AzureResourceTypeCatalog.Classify("Microsoft.Network/firewallPolicies");

        result.FriendlyName.Should().Be("Firewall Policy");
        result.ServiceType.Should().Be("external");
        result.C4Level.Should().Be("Container");
        result.IncludeInDiagram.Should().BeFalse();
    }
}
