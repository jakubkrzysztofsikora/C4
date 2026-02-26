using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class CompositeDiscoveryInputProvider(IEnumerable<IDiscoverySourceAdapter> sourceAdapters) : IDiscoveryInputProvider
{
    public async Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
    {
        var requestedSources = request.Sources.Distinct().ToHashSet();

        var resources = new Dictionary<string, DiscoveryResourceDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceAdapter in sourceAdapters.Where(adapter => requestedSources.Contains(adapter.Source)))
        {
            var discovered = await sourceAdapter.GetResourcesAsync(request, cancellationToken);
            foreach (var resource in discovered)
            {
                resources[resource.ResourceId] = resource;
            }
        }

        return resources.Values.ToArray();
    }
}
