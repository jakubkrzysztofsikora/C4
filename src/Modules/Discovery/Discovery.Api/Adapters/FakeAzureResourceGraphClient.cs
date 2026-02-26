using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class FakeAzureResourceGraphClient : IAzureResourceGraphClient
{
    public Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        var rg = $"/subscriptions/{externalSubscriptionId}/resourceGroups/platform/providers";

        var resources = new AzureResourceRecord[]
        {
            new($"{rg}/Microsoft.Web/sites/frontend", "Microsoft.Web/sites", "frontend", null),
            new($"{rg}/Microsoft.Web/sites/api", "Microsoft.Web/sites", "api", null),
            new($"{rg}/Microsoft.Web/sites/functions/processor", "Microsoft.Web/sites/functions", "processor", $"{rg}/Microsoft.Web/sites/api"),
            new($"{rg}/Microsoft.DBforPostgreSQL/flexibleServers/pg", "Microsoft.DBforPostgreSQL/flexibleServers", "pg", $"{rg}/Microsoft.Web/sites/api"),
            new($"{rg}/Microsoft.Cache/Redis/cache", "Microsoft.Cache/Redis", "cache", null),
            new($"{rg}/Microsoft.ServiceBus/namespaces/bus", "Microsoft.ServiceBus/namespaces", "bus", null),
            new($"{rg}/Microsoft.KeyVault/vaults/secrets", "Microsoft.KeyVault/vaults", "secrets", null),
            new($"{rg}/Microsoft.Network/networkSecurityGroups/nsg", "Microsoft.Network/networkSecurityGroups", "nsg", null),
            new($"{rg}/Microsoft.Network/networkInterfaces/nic", "Microsoft.Network/networkInterfaces", "nic", null),
            new($"{rg}/Microsoft.Compute/disks/osdisk", "Microsoft.Compute/disks", "osdisk", null),
            new($"{rg}/Microsoft.Network/publicIPAddresses/pip", "Microsoft.Network/publicIPAddresses", "pip", null),
            new($"{rg}/Microsoft.ManagedIdentity/userAssignedIdentities/mi", "Microsoft.ManagedIdentity/userAssignedIdentities", "mi", null),
        };

        return Task.FromResult<IReadOnlyCollection<AzureResourceRecord>>(resources);
    }
}
