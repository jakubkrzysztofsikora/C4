namespace C4.Modules.Discovery.Domain.Resources;

public static class AzureResourceTypeCatalog
{
    private static readonly Dictionary<string, AzureResourceClassification> KnownTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Microsoft.Web/sites"] = new("App Service", "app", "Container", true),
            ["Microsoft.Web/sites/functions"] = new("Function App", "app", "Component", true),
            ["Microsoft.DBforPostgreSQL/flexibleServers"] = new("PostgreSQL", "database", "Container", true),
            ["Microsoft.Sql/servers"] = new("SQL Server", "database", "Container", true),
            ["Microsoft.DocumentDB/databaseAccounts"] = new("Cosmos DB", "database", "Container", true),
            ["Microsoft.Cache/Redis"] = new("Redis Cache", "cache", "Container", true),
            ["Microsoft.ServiceBus/namespaces"] = new("Service Bus", "queue", "Container", true),
            ["Microsoft.Storage/storageAccounts"] = new("Storage Account", "external", "Container", true),
            ["Microsoft.KeyVault/vaults"] = new("Key Vault", "external", "Container", true),
            ["Microsoft.ContainerService/managedClusters"] = new("AKS Cluster", "app", "Container", true),
            ["Microsoft.App/containerApps"] = new("Container App", "app", "Container", true),
            ["Microsoft.ApiManagement/service"] = new("API Management", "api", "Container", true),
            ["Microsoft.SignalRService/SignalR"] = new("SignalR", "api", "Component", true),
            ["Microsoft.Network/networkInterfaces"] = new("NIC", "external", "Component", false),
            ["Microsoft.Network/networkSecurityGroups"] = new("NSG", "external", "Component", false),
            ["Microsoft.Compute/disks"] = new("Disk", "external", "Component", false),
            ["Microsoft.Network/publicIPAddresses"] = new("Public IP", "external", "Component", false),
            ["Microsoft.ManagedIdentity/userAssignedIdentities"] = new("Managed Identity", "external", "Component", false),
            ["Microsoft.Insights/diagnosticSettings"] = new("Diagnostic Settings", "external", "Component", false),
            ["Microsoft.Insights/components"] = new("Application Insights", "external", "Container", true),
        };

    public static AzureResourceClassification Classify(string armResourceType)
    {
        if (KnownTypes.TryGetValue(armResourceType, out var classification))
            return classification;

        var shortName = armResourceType.Split('/').Last();
        return new AzureResourceClassification(shortName, "external", "Container", true);
    }
}
