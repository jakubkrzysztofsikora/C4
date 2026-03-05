using System.Text.Json;
using System.Text.Json.Serialization;
using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class DiscoveryApplicationInsightsTokenProvider(
    IAzureSubscriptionRepository subscriptionRepository,
    IAzureTokenStore tokenStore,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<DiscoveryApplicationInsightsTokenProvider> logger)
    : IApplicationInsightsTokenProvider
{
    private const string AppInsightsScope = "https://api.applicationinsights.io/.default offline_access";

    private string TenantId => configuration["AzureAd:TenantId"] ?? string.Empty;
    private string ClientId => configuration["AzureAd:ClientId"] ?? string.Empty;
    private string ClientSecret => configuration["AzureAd:ClientSecret"] ?? string.Empty;

    public async Task<string?> GetAccessTokenAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(TenantId)
            || string.IsNullOrWhiteSpace(ClientId)
            || string.IsNullOrWhiteSpace(ClientSecret))
        {
            logger.LogWarning(
                "Cannot request Application Insights delegated token for project {ProjectId}: AzureAd credentials are not fully configured",
                projectId);
            return null;
        }

        var externalSubscriptionId = await ResolveExternalSubscriptionIdAsync(projectId, cancellationToken);
        if (string.IsNullOrWhiteSpace(externalSubscriptionId))
        {
            logger.LogDebug(
                "No connected Azure subscription found for project {ProjectId}; cannot resolve Application Insights delegated token",
                projectId);
            return null;
        }

        var tokenInfo = await tokenStore.GetAsync(externalSubscriptionId, cancellationToken);
        if (tokenInfo is null)
        {
            logger.LogDebug(
                "No Azure token found for subscription {ExternalSubscriptionId}; cannot resolve Application Insights delegated token",
                externalSubscriptionId);
            return null;
        }

        if (string.IsNullOrWhiteSpace(tokenInfo.RefreshToken))
        {
            logger.LogWarning(
                "Azure token for subscription {ExternalSubscriptionId} does not contain refresh token; reconnect Azure subscription to enable delegated telemetry auth",
                externalSubscriptionId);
            return null;
        }

        var refreshed = await RefreshForAppInsightsScopeAsync(tokenInfo.RefreshToken, cancellationToken);
        if (refreshed is null)
            return null;

        if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken)
            && !string.Equals(refreshed.RefreshToken, tokenInfo.RefreshToken, StringComparison.Ordinal))
        {
            await tokenStore.StoreAsync(
                externalSubscriptionId,
                tokenInfo with { RefreshToken = refreshed.RefreshToken },
                cancellationToken);
        }

        return refreshed.AccessToken;
    }

    private async Task<string?> ResolveExternalSubscriptionIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        // Subscription-to-project mapping is currently one connected subscription in practice.
        // Use first connected subscription as delegated auth source until explicit mapping is added.
        var subscription = await subscriptionRepository.GetFirstAsync(cancellationToken);
        return subscription?.ExternalSubscriptionId;
    }

    private async Task<RefreshedToken?> RefreshForAppInsightsScopeAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["scope"] = AppInsightsScope
            });

            var response = await client.PostAsync(
                $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token",
                content,
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Application Insights delegated token refresh failed ({StatusCode}): {Response}",
                    response.StatusCode,
                    json);
                return null;
            }

            var token = JsonSerializer.Deserialize<TokenEndpointResponse>(json);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                logger.LogWarning("Application Insights delegated token refresh returned empty access token");
                return null;
            }

            return new RefreshedToken(token.AccessToken, token.RefreshToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Application Insights delegated token refresh threw an unexpected error");
            return null;
        }
    }

    private sealed record TokenEndpointResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);

    private sealed record RefreshedToken(string AccessToken, string? RefreshToken);
}
