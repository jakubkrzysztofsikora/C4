using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class RemoteMcpDiscoverySourceAdapter : IDiscoverySourceAdapter
{
    public DiscoverySourceKind Source => DiscoverySourceKind.RemoteMcp;

    public Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<DiscoveryResourceDescriptor>>([]);
    }
}
