using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.Configuration;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class AzureIdentityService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IAzureIdentityService
{
    private string TenantId => configuration["AzureAd:TenantId"] ?? throw new InvalidOperationException("AzureAd:TenantId not configured");
    private string ClientId => configuration["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
    private string ClientSecret => configuration["AzureAd:ClientSecret"] ?? throw new InvalidOperationException("AzureAd:ClientSecret not configured");

    private const string ManagementScope = "https://management.azure.com/user_impersonation offline_access";

    public string BuildAuthorizationUrl(string redirectUri, string state)
    {
        var scope = Uri.EscapeDataString(ManagementScope);
        return $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/authorize" +
               $"?client_id={ClientId}" +
               $"&response_type=code" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&scope={scope}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&response_mode=query";
    }

    public async Task<AzureTokenResponse> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["scope"] = ManagementScope
        });

        var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token",
            content,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure token exchange failed ({response.StatusCode}): {json}");
        }
        var tokenResult = JsonSerializer.Deserialize<TokenEndpointResponse>(json)!;

        return new AzureTokenResponse(tokenResult.AccessToken, tokenResult.ExpiresIn, tokenResult.RefreshToken);
    }

    public async Task<AzureTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["scope"] = ManagementScope
        });

        var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token",
            content,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure token refresh failed ({response.StatusCode}): {json}");
        }
        var tokenResult = JsonSerializer.Deserialize<TokenEndpointResponse>(json)!;

        return new AzureTokenResponse(tokenResult.AccessToken, tokenResult.ExpiresIn, tokenResult.RefreshToken);
    }

    public async Task<IReadOnlyList<AzureSubscriptionInfo>> ListSubscriptionsAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(
            "https://management.azure.com/subscriptions?api-version=2022-12-01",
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure subscription listing failed ({response.StatusCode}): {json}");
        }
        var result = JsonSerializer.Deserialize<SubscriptionListResponse>(json)!;

        return result.Value
            .Select(s => new AzureSubscriptionInfo(s.SubscriptionId, s.DisplayName, s.State))
            .ToList();
    }

    private sealed record TokenEndpointResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);

    private sealed record SubscriptionListResponse(
        [property: JsonPropertyName("value")] IReadOnlyList<SubscriptionEntry> Value);

    private sealed record SubscriptionEntry(
        [property: JsonPropertyName("subscriptionId")] string SubscriptionId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("state")] string State);
}
