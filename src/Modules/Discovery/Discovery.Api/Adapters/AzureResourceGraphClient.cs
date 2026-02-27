using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class AzureResourceGraphClient(
    IHttpClientFactory httpClientFactory,
    IAzureTokenStore tokenStore,
    IAzureIdentityService identityService,
    ILogger<AzureResourceGraphClient> logger) : IAzureResourceGraphClient
{
    private const string ResourceGraphEndpoint = "https://management.azure.com/providers/Microsoft.ResourceGraph/resources?api-version=2021-06-01";

    private const string ResourceQuery = "Resources | project id, type, name, properties";

    public async Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        string accessToken = await ResolveAccessTokenAsync(externalSubscriptionId, cancellationToken);

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var requestBody = new ResourceGraphRequest([externalSubscriptionId], ResourceQuery);
        string json = JsonSerializer.Serialize(requestBody);
        StringContent content = new(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ResourceGraphEndpoint, content, cancellationToken);
        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Azure Resource Graph query failed ({StatusCode}): {Response}", response.StatusCode, responseJson);
            throw new InvalidOperationException($"Azure Resource Graph query failed ({response.StatusCode})");
        }

        return ParseResourceGraphResponse(responseJson);
    }

    private async Task<string> ResolveAccessTokenAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        AzureTokenInfo? tokenInfo = await tokenStore.GetAsync(externalSubscriptionId, cancellationToken);

        if (tokenInfo is null)
            throw new InvalidOperationException("No Azure credentials found. Please re-authenticate with Azure.");

        if (tokenInfo.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            return tokenInfo.AccessToken;

        if (tokenInfo.RefreshToken is null)
            throw new InvalidOperationException("Azure token expired and no refresh token available. Please re-authenticate.");

        logger.LogInformation("Azure access token expired for subscription {SubscriptionId}, refreshing", externalSubscriptionId);
        AzureTokenResponse refreshed = await identityService.RefreshTokenAsync(tokenInfo.RefreshToken, cancellationToken);
        AzureTokenInfo newTokenInfo = new(refreshed.AccessToken, refreshed.RefreshToken ?? tokenInfo.RefreshToken, DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn));
        await tokenStore.StoreAsync(externalSubscriptionId, newTokenInfo, cancellationToken);
        return refreshed.AccessToken;
    }

    private static IReadOnlyCollection<AzureResourceRecord> ParseResourceGraphResponse(string responseJson)
    {
        using JsonDocument document = JsonDocument.Parse(responseJson);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("data", out JsonElement data))
            return [];

        if (data.TryGetProperty("columns", out JsonElement columns) && data.TryGetProperty("rows", out JsonElement rows))
            return ParseTabularResponse(columns, rows);

        return [];
    }

    private static IReadOnlyCollection<AzureResourceRecord> ParseTabularResponse(JsonElement columns, JsonElement rows)
    {
        string[] columnNames = columns.EnumerateArray()
            .Select(c => c.GetProperty("name").GetString() ?? string.Empty)
            .ToArray();

        int idIndex = Array.IndexOf(columnNames, "id");
        int typeIndex = Array.IndexOf(columnNames, "type");
        int nameIndex = Array.IndexOf(columnNames, "name");
        int propsIndex = Array.IndexOf(columnNames, "properties");

        if (idIndex < 0 || typeIndex < 0 || nameIndex < 0)
            return [];

        List<AzureResourceRecord> results = [];
        foreach (JsonElement row in rows.EnumerateArray())
        {
            JsonElement[] cells = row.EnumerateArray().ToArray();
            string resourceId = cells[idIndex].GetString() ?? string.Empty;
            string resourceType = cells[typeIndex].GetString() ?? string.Empty;
            string name = cells[nameIndex].GetString() ?? string.Empty;

            string? parentResourceId = null;
            if (propsIndex >= 0 && cells[propsIndex].ValueKind == JsonValueKind.Object)
            {
                if (cells[propsIndex].TryGetProperty("parentResourceId", out JsonElement parentProp))
                    parentResourceId = parentProp.GetString();
            }

            if (!string.IsNullOrWhiteSpace(resourceId))
                results.Add(new AzureResourceRecord(resourceId, resourceType, name, parentResourceId));
        }

        return results;
    }

    private sealed record ResourceGraphRequest(
        [property: JsonPropertyName("subscriptions")] string[] Subscriptions,
        [property: JsonPropertyName("query")] string Query);
}
