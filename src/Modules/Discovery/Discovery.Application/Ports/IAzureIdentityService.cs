namespace C4.Modules.Discovery.Application.Ports;

public interface IAzureIdentityService
{
    string BuildAuthorizationUrl(string redirectUri, string state);
    Task<AzureTokenResponse> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task<IReadOnlyList<AzureSubscriptionInfo>> ListSubscriptionsAsync(string accessToken, CancellationToken cancellationToken);
}

public sealed record AzureTokenResponse(string AccessToken, int ExpiresIn);

public sealed record AzureSubscriptionInfo(string SubscriptionId, string DisplayName, string State);
