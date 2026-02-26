namespace C4.Modules.Discovery.Application.Ports;

public interface IDiscoverySourceAdapter
{
    DiscoverySourceKind Source { get; }

    Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken);
}
