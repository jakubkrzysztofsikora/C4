namespace C4.Modules.Discovery.Domain.Resources;

public static class AzureResourceTypeCatalog
{
    private static readonly Dictionary<string, AzureResourceClassification> KnownTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Microsoft.Web/sites"] = new("App Service", "app", "Container", true),
            ["Microsoft.Web/sites/functions"] = new("Function App", "app", "Component", true),
            ["Microsoft.Web/sites/slots"] = new("Deployment Slot", "app", "Component", true),
            ["Microsoft.Web/staticSites"] = new("Static Web App", "app", "Container", true),
            ["Microsoft.DBforPostgreSQL/flexibleServers"] = new("PostgreSQL", "database", "Container", true),
            ["Microsoft.Sql/servers"] = new("SQL Server", "database", "Container", true),
            ["Microsoft.Sql/servers/databases"] = new("SQL Database", "database", "Component", true),
            ["Microsoft.DocumentDB/databaseAccounts"] = new("Cosmos DB", "database", "Container", true),
            ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases"] = new("Cosmos Database", "database", "Component", true),
            ["Microsoft.Cache/Redis"] = new("Redis Cache", "cache", "Container", true),
            ["Microsoft.ServiceBus/namespaces"] = new("Service Bus", "queue", "Container", true),
            ["Microsoft.ServiceBus/namespaces/topics"] = new("Service Bus Topic", "queue", "Component", true),
            ["Microsoft.ServiceBus/namespaces/queues"] = new("Service Bus Queue", "queue", "Component", true),
            ["Microsoft.Storage/storageAccounts"] = new("Storage Account", "external", "Container", true),
            ["Microsoft.Storage/storageAccounts/blobServices"] = new("Blob Storage", "external", "Component", true),
            ["Microsoft.Storage/storageAccounts/tableServices"] = new("Table Storage", "external", "Component", true),
            ["Microsoft.KeyVault/vaults"] = new("Key Vault", "external", "Container", true),
            ["Microsoft.ContainerService/managedClusters"] = new("AKS Cluster", "app", "Container", true),
            ["Microsoft.ContainerRegistry/registries"] = new("Container Registry", "external", "Container", true),
            ["Microsoft.App/containerApps"] = new("Container App", "app", "Container", true),
            ["Microsoft.ApiManagement/service"] = new("API Management", "api", "Container", true),
            ["Microsoft.SignalRService/SignalR"] = new("SignalR", "api", "Component", true),
            ["Microsoft.EventHub/namespaces"] = new("Event Hub", "queue", "Container", true),
            ["Microsoft.CognitiveServices/accounts"] = new("Cognitive Services", "api", "Container", true),
            ["Microsoft.Cdn/profiles"] = new("CDN Profile", "external", "Container", true),
            ["Microsoft.Network/virtualNetworks"] = new("Virtual Network", "external", "Container", false),
            ["Microsoft.Network/loadBalancers"] = new("Load Balancer", "external", "Container", false),
            ["Microsoft.Network/networkInterfaces"] = new("NIC", "external", "Component", false),
            ["Microsoft.Network/networkSecurityGroups"] = new("NSG", "external", "Component", false),
            ["Microsoft.Compute/disks"] = new("Disk", "external", "Component", false),
            ["Microsoft.Network/publicIPAddresses"] = new("Public IP", "external", "Component", false),
            ["Microsoft.ManagedIdentity/userAssignedIdentities"] = new("Managed Identity", "external", "Component", false),
            ["Microsoft.Insights/diagnosticSettings"] = new("Diagnostic Settings", "external", "Component", false),
            ["Microsoft.Insights/components"] = new("Application Insights", "external", "Container", true),
            ["Microsoft.OperationalInsights/workspaces"] = new("Log Analytics", "external", "Container", false),
        };

    public static bool IsKnown(string armResourceType) => KnownTypes.ContainsKey(armResourceType);

    public static AzureResourceClassification Classify(string armResourceType)
    {
        if (KnownTypes.TryGetValue(armResourceType, out var classification))
            return classification;

        var shortName = armResourceType.Split('/').Last();
        return new AzureResourceClassification(shortName, "external", "Container", true);
    }
}
