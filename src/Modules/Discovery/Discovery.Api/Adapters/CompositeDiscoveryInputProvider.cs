using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class CompositeDiscoveryInputProvider(IEnumerable<IDiscoverySourceAdapter> sourceAdapters) : IDiscoveryInputProvider
{
    public async Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
    {
        var requestedSources = request.Sources.Distinct().ToHashSet();

        var tasks = sourceAdapters
            .Where(adapter => requestedSources.Contains(adapter.Source))
            .Select(adapter => adapter.GetResourcesAsync(request, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var resources = new Dictionary<string, DiscoveryResourceDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var discovered in results)
        {
            foreach (var resource in discovered)
            {
                resources[resource.ResourceId] = resource;
            }
        }

        return resources.Values.ToArray();
    }
}
