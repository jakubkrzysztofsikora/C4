using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class FakeAzureResourceGraphClient : IAzureResourceGraphClient
{
    public Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        var resources = new AzureResourceRecord[]
        {
            new($"/subscriptions/{externalSubscriptionId}/resourceGroups/platform/providers/Microsoft.Web/sites/frontend", "Microsoft.Web/sites", "frontend", null),
            new($"/subscriptions/{externalSubscriptionId}/resourceGroups/platform/providers/Microsoft.Web/sites/api", "Microsoft.Web/sites", "api", null),
            new($"/subscriptions/{externalSubscriptionId}/resourceGroups/platform/providers/Microsoft.DBforPostgreSQL/flexibleServers/pg", "Microsoft.DBforPostgreSQL/flexibleServers", "pg", $"/subscriptions/{externalSubscriptionId}/resourceGroups/platform/providers/Microsoft.Web/sites/api")
        };

        return Task.FromResult<IReadOnlyCollection<AzureResourceRecord>>(resources);
    }
}
