using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class AzureSubscriptionDiscoverySourceAdapter(IAzureResourceGraphClient resourceGraphClient) : IDiscoverySourceAdapter
{
    public DiscoverySourceKind Source => DiscoverySourceKind.AzureSubscription;

    public async Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalSubscriptionId))
            return Array.Empty<DiscoveryResourceDescriptor>();

        var records = await resourceGraphClient.GetResourcesAsync(request.ExternalSubscriptionId, cancellationToken);
        return records
            .Select(record => new DiscoveryResourceDescriptor(
                record.ResourceId,
                record.ResourceType,
                record.Name,
                record.ParentResourceId,
                Source,
                record.AppInsightsAppId,
                record.PropertyReferences,
                record.ResourceGroup,
                record.Tags))
            .ToArray();
    }
}
