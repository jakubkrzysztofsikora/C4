namespace C4.Modules.Discovery.Application.Ports;

public interface IDiscoveryInputProvider
{
    Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken);
}

public sealed record NormalizedDiscoveryRequest(
    Guid ProjectId,
    string? OrganizationId,
    string? ExternalSubscriptionId,
    IReadOnlyCollection<DiscoverySourceKind> Sources);

public enum DiscoverySourceKind
{
    AzureSubscription,
    RepositoryIac,
    RemoteMcp
}

public static class DiscoverySourceKindDefaults
{
    public static readonly IReadOnlyCollection<DiscoverySourceKind> All = (IReadOnlyCollection<DiscoverySourceKind>)Enum.GetValues<DiscoverySourceKind>();
}

public sealed record DiscoveryResourceDescriptor(
    string ResourceId,
    string ResourceType,
    string Name,
    string? ParentResourceId,
    DiscoverySourceKind Source,
    string? AppInsightsAppId = null,
    IReadOnlyCollection<string>? PropertyReferences = null,
    string? ResourceGroup = null,
    IReadOnlyDictionary<string, string>? Tags = null);
