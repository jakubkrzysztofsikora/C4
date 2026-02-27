namespace C4.Modules.Discovery.Application.Ports;

public interface IAzureTokenStore
{
    Task StoreAsync(string externalSubscriptionId, AzureTokenInfo tokenInfo, CancellationToken cancellationToken);
    Task<AzureTokenInfo?> GetAsync(string externalSubscriptionId, CancellationToken cancellationToken);
}

public sealed record AzureTokenInfo(string AccessToken, string? RefreshToken, DateTime ExpiresAtUtc);
