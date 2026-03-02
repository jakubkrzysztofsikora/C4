namespace C4.Modules.Discovery.Application.Ports;

public interface IAzureResourceGraphClient
{
    Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken);
}

public sealed record AzureResourceRecord(
    string ResourceId,
    string ResourceType,
    string Name,
    string? ParentResourceId,
    string? AppInsightsAppId = null,
    IReadOnlyCollection<string>? PropertyReferences = null,
    string? ResourceGroup = null,
    IReadOnlyDictionary<string, string>? Tags = null);
