namespace C4.Modules.Discovery.Domain.Resources;

public static class AzureResourceTypeCatalog
{
    private static readonly Dictionary<string, AzureResourceClassification> KnownTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Context-level: System boundaries and grouping constructs
            ["Microsoft.Resources/subscriptions"] = new("Subscription", "boundary", "Context", false),
            ["Microsoft.Resources/subscriptions/resourceGroups"] = new("Resource Group", "boundary", "Context", true),
            ["Microsoft.Resources/resourceGroups"] = new("Resource Group", "boundary", "Context", true),
            ["Microsoft.Network/virtualNetworks"] = new("Virtual Network", "boundary", "Context", true),

            // Container-level: Compute / App services
            ["Microsoft.Web/sites"] = new("App Service", "app", "Container", true),
            ["Microsoft.Web/staticSites"] = new("Static Web App", "app", "Container", true),
            ["Microsoft.ContainerService/managedClusters"] = new("AKS Cluster", "app", "Container", true),
            ["Microsoft.App/containerApps"] = new("Container App", "app", "Container", true),
            ["Microsoft.App/managedEnvironments"] = new("Container App Environment", "app", "Container", true),
            ["Microsoft.Compute/virtualMachines"] = new("Virtual Machine", "app", "Container", true),
            ["Microsoft.Compute/virtualMachineScaleSets"] = new("VM Scale Set", "app", "Container", true),
            ["Microsoft.Batch/batchAccounts"] = new("Batch Account", "app", "Container", true),

            // Container-level: API / Integration services
            ["Microsoft.ApiManagement/service"] = new("API Management", "api", "Container", true),
            ["Microsoft.SignalRService/SignalR"] = new("SignalR", "api", "Container", true),
            ["Microsoft.Web/connections"] = new("API Connection", "api", "Component", true),

            // Container-level: Database services
            ["Microsoft.DBforPostgreSQL/flexibleServers"] = new("PostgreSQL", "database", "Container", true),
            ["Microsoft.DBforPostgreSQL/servers"] = new("PostgreSQL (Single)", "database", "Container", true),
            ["Microsoft.DBforMySQL/flexibleServers"] = new("MySQL", "database", "Container", true),
            ["Microsoft.Sql/servers"] = new("SQL Server", "database", "Container", true),
            ["Microsoft.DocumentDB/databaseAccounts"] = new("Cosmos DB", "database", "Container", true),
            ["Microsoft.Cache/Redis"] = new("Redis Cache", "cache", "Container", true),
            ["Microsoft.Search/searchServices"] = new("Azure Search", "database", "Container", true),

            // Container-level: Messaging / Queue services
            ["Microsoft.ServiceBus/namespaces"] = new("Service Bus", "queue", "Container", true),
            ["Microsoft.EventHub/namespaces"] = new("Event Hub", "queue", "Container", true),
            ["Microsoft.EventGrid/topics"] = new("Event Grid Topic", "queue", "Container", true),
            ["Microsoft.EventGrid/domains"] = new("Event Grid Domain", "queue", "Container", true),
            ["Microsoft.EventGrid/systemTopics"] = new("Event Grid System Topic", "queue", "Container", true),

            // Container-level: Storage services
            ["Microsoft.Storage/storageAccounts"] = new("Storage Account", "storage", "Container", true),
            ["Microsoft.KeyVault/vaults"] = new("Key Vault", "storage", "Container", true),
            ["Microsoft.ContainerRegistry/registries"] = new("Container Registry", "storage", "Container", true),

            // Container-level: AI / Cognitive services
            ["Microsoft.CognitiveServices/accounts"] = new("Cognitive Services", "api", "Container", true),
            ["Microsoft.MachineLearningServices/workspaces"] = new("ML Workspace", "api", "Container", true),

            // Container-level: CDN / Networking (user-facing)
            ["Microsoft.Cdn/profiles"] = new("CDN Profile", "external", "Container", true),
            ["Microsoft.Network/frontDoors"] = new("Front Door", "external", "Container", true),
            ["Microsoft.Network/applicationGateways"] = new("Application Gateway", "external", "Container", true),
            ["Microsoft.Network/loadBalancers"] = new("Load Balancer", "external", "Container", false),
            ["Microsoft.Network/trafficManagerProfiles"] = new("Traffic Manager", "external", "Container", true),
            ["Microsoft.Network/dnszones"] = new("DNS Zone", "external", "Container", false),

            // Container-level: Monitoring / Observability
            ["Microsoft.Insights/components"] = new("Application Insights", "monitoring", "Container", true),
            ["Microsoft.OperationalInsights/workspaces"] = new("Log Analytics", "monitoring", "Container", false),
            ["Microsoft.AlertsManagement/smartDetectorAlertRules"] = new("Smart Detection", "monitoring", "Component", false),

            // Container-level: Logic / Workflow
            ["Microsoft.Logic/workflows"] = new("Logic App", "app", "Container", true),
            ["Microsoft.Automation/automationAccounts"] = new("Automation Account", "app", "Container", true),

            // Component-level: Sub-resources within containers
            ["Microsoft.Web/sites/functions"] = new("Function App", "app", "Component", true),
            ["Microsoft.Web/sites/slots"] = new("Deployment Slot", "app", "Component", true),
            ["Microsoft.Web/serverfarms"] = new("App Service Plan", "app", "Component", false),
            ["Microsoft.Sql/servers/databases"] = new("SQL Database", "database", "Component", true),
            ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases"] = new("Cosmos Database", "database", "Component", true),
            ["Microsoft.DocumentDB/databaseAccounts/mongodbDatabases"] = new("Cosmos MongoDB", "database", "Component", true),
            ["Microsoft.ServiceBus/namespaces/topics"] = new("Service Bus Topic", "queue", "Component", true),
            ["Microsoft.ServiceBus/namespaces/queues"] = new("Service Bus Queue", "queue", "Component", true),
            ["Microsoft.EventHub/namespaces/eventhubs"] = new("Event Hub Instance", "queue", "Component", true),
            ["Microsoft.Storage/storageAccounts/blobServices"] = new("Blob Storage", "storage", "Component", true),
            ["Microsoft.Storage/storageAccounts/tableServices"] = new("Table Storage", "storage", "Component", true),
            ["Microsoft.Storage/storageAccounts/queueServices"] = new("Queue Storage", "queue", "Component", true),
            ["Microsoft.Storage/storageAccounts/fileServices"] = new("File Storage", "storage", "Component", true),

            // Component-level: Network infrastructure
            ["Microsoft.Network/networkInterfaces"] = new("NIC", "external", "Component", false),
            ["Microsoft.Network/networkSecurityGroups"] = new("NSG", "external", "Component", false),
            ["Microsoft.Network/publicIPAddresses"] = new("Public IP", "external", "Component", false),
            ["Microsoft.Network/virtualNetworks/subnets"] = new("Subnet", "external", "Component", false),
            ["Microsoft.Network/privateDnsZones"] = new("Private DNS Zone", "external", "Component", false),
            ["Microsoft.Network/privateEndpoints"] = new("Private Endpoint", "external", "Component", false),

            // Component-level: Identity / Security
            ["Microsoft.ManagedIdentity/userAssignedIdentities"] = new("Managed Identity", "external", "Component", false),
            ["Microsoft.Authorization/roleAssignments"] = new("Role Assignment", "external", "Component", false),

            // Component-level: Compute sub-resources
            ["Microsoft.Compute/disks"] = new("Disk", "external", "Component", false),

            // Component-level: Diagnostics
            ["Microsoft.Insights/diagnosticSettings"] = new("Diagnostic Settings", "monitoring", "Component", false),
            ["Microsoft.Insights/actionGroups"] = new("Action Group", "monitoring", "Component", false),
            ["Microsoft.Insights/metricAlerts"] = new("Metric Alert", "monitoring", "Component", false),
            ["Microsoft.Insights/activityLogAlerts"] = new("Activity Log Alert", "monitoring", "Component", false),
        };

    public static bool IsKnown(string armResourceType) => KnownTypes.ContainsKey(armResourceType);

    public static AzureResourceClassification Classify(string armResourceType)
    {
        if (KnownTypes.TryGetValue(armResourceType, out var classification))
            return classification;

        var segments = armResourceType.Split('/');
        var isSubResource = segments.Length > 2;
        var shortName = segments.Last();
        var level = isSubResource ? "Component" : "Container";

        return new AzureResourceClassification(shortName, "external", level, !isSubResource);
    }
}
