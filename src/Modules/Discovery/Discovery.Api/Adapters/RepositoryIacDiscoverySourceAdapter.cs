using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class RepositoryIacDiscoverySourceAdapter : IDiscoverySourceAdapter
{
    public DiscoverySourceKind Source => DiscoverySourceKind.RepositoryIac;

    public Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<DiscoveryResourceDescriptor>>([]);
    }
}
