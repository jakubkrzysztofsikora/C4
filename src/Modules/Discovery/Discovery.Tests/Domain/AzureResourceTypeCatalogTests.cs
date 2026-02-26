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
}
