using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class AzureResourceGraphClient(
    IHttpClientFactory httpClientFactory,
    IAzureTokenStore tokenStore,
    IAzureIdentityService identityService,
    IConfiguration configuration,
    ILogger<AzureResourceGraphClient> logger) : IAzureResourceGraphClient
{
    private const string ResourceGraphEndpoint = "https://management.azure.com/providers/Microsoft.ResourceGraph/resources?api-version=2024-04-01";
    private const string ManagementScope = "https://management.azure.com/.default";

    private const string ResourceQuery = "Resources | project id, type, name, properties, resourceGroup, tags | union (ResourceContainers | where type =~ 'microsoft.resources/subscriptions/resourcegroups' | project id, type, name, properties, resourceGroup=name, tags)";

    public async Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        string accessToken = await ResolveAccessTokenAsync(externalSubscriptionId, cancellationToken);

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        List<AzureResourceRecord> allResults = [];
        string? skipToken = null;

        do
        {
            var requestBody = new ResourceGraphRequest(
                [externalSubscriptionId],
                ResourceQuery,
                new ResourceGraphOptions(1000, skipToken));

            string json = JsonSerializer.Serialize(requestBody);
            using StringContent content = new(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(ResourceGraphEndpoint, content, cancellationToken);
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string detail = ExtractAzureErrorDetail(responseJson);
                logger.LogError("Azure Resource Graph query failed ({StatusCode}): {Response}", response.StatusCode, responseJson);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    throw new UnauthorizedAccessException($"Azure returned {(int)response.StatusCode}: {detail}");

                throw new HttpRequestException($"Azure Resource Graph query failed ({(int)response.StatusCode}): {detail}", null, response.StatusCode);
            }

            var page = ParseResourceGraphPage(responseJson);
            allResults.AddRange(page.Records);
            skipToken = page.SkipToken;

            logger.LogInformation("Fetched {Count} resources from Azure Resource Graph (total so far: {Total}, has more: {HasMore})",
                page.Records.Count, allResults.Count, skipToken is not null);
        } while (skipToken is not null);

        return allResults;
    }

    private async Task<string> ResolveAccessTokenAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        AzureTokenInfo? tokenInfo = await tokenStore.GetAsync(externalSubscriptionId, cancellationToken);

        if (tokenInfo is null)
        {
            var fallbackToken = await TryGetClientCredentialsAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallbackToken))
                return fallbackToken;

            throw new InvalidOperationException("No Azure credentials found. Please re-authenticate with Azure.");
        }

        if (tokenInfo.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            return tokenInfo.AccessToken;

        if (tokenInfo.RefreshToken is null)
        {
            var fallbackToken = await TryGetClientCredentialsAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallbackToken))
                return fallbackToken;

            throw new InvalidOperationException("Azure token expired and no refresh token available. Please re-authenticate.");
        }

        try
        {
            logger.LogInformation("Azure access token expired for subscription {SubscriptionId}, refreshing", externalSubscriptionId);
            AzureTokenResponse refreshed = await identityService.RefreshTokenAsync(tokenInfo.RefreshToken, cancellationToken);
            AzureTokenInfo newTokenInfo = new(refreshed.AccessToken, refreshed.RefreshToken ?? tokenInfo.RefreshToken, DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn));
            await tokenStore.StoreAsync(externalSubscriptionId, newTokenInfo, cancellationToken);
            return refreshed.AccessToken;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Azure delegated token refresh failed for subscription {SubscriptionId}; trying client-credentials fallback",
                externalSubscriptionId);

            var fallbackToken = await TryGetClientCredentialsAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallbackToken))
                return fallbackToken;

            throw;
        }
    }

    private async Task<string?> TryGetClientCredentialsAccessTokenAsync(CancellationToken cancellationToken)
    {
        var tenantId = configuration["AzureAd:TenantId"] ?? string.Empty;
        var clientId = configuration["AzureAd:ClientId"] ?? string.Empty;
        var clientSecret = configuration["AzureAd:ClientSecret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        using var client = httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = ManagementScope
        });

        var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            content,
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Azure client-credentials token request failed ({StatusCode}): {Response}", response.StatusCode, json);
            return null;
        }

        var token = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(json);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            return null;

        return token.AccessToken;
    }

    private static ResourceGraphPage ParseResourceGraphPage(string responseJson)
    {
        using JsonDocument document = JsonDocument.Parse(responseJson);
        JsonElement root = document.RootElement;

        string? skipToken = root.TryGetProperty("$skipToken", out JsonElement skipTokenProp)
            ? skipTokenProp.GetString()
            : null;

        if (!root.TryGetProperty("data", out JsonElement data))
            return new ResourceGraphPage([], skipToken);

        IReadOnlyCollection<AzureResourceRecord> records;

        if (data.ValueKind == JsonValueKind.Array)
            records = ParseObjectArrayResponse(data);
        else if (data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("columns", out JsonElement columns)
            && data.TryGetProperty("rows", out JsonElement rows))
            records = ParseTabularResponse(columns, rows);
        else
            records = [];

        return new ResourceGraphPage(records, skipToken);
    }

    private static IReadOnlyCollection<AzureResourceRecord> ParseObjectArrayResponse(JsonElement data)
    {
        List<AzureResourceRecord> results = [];
        foreach (JsonElement element in data.EnumerateArray())
        {
            string resourceId = element.TryGetProperty("id", out JsonElement idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
            string resourceType = element.TryGetProperty("type", out JsonElement typeProp) ? typeProp.GetString() ?? string.Empty : string.Empty;
            string name = element.TryGetProperty("name", out JsonElement nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
            string? resourceGroup = element.TryGetProperty("resourceGroup", out JsonElement rgProp) ? rgProp.GetString() : null;
            IReadOnlyDictionary<string, string>? tags = element.TryGetProperty("tags", out JsonElement tagsProp)
                ? ParseTags(tagsProp)
                : null;

            string? parentResourceId = null;
            string? appInsightsAppId = null;
            IReadOnlyCollection<string> propertyRefs = Array.Empty<string>();
            if (element.TryGetProperty("properties", out JsonElement props) && props.ValueKind == JsonValueKind.Object)
            {
                if (props.TryGetProperty("parentResourceId", out JsonElement parentProp))
                    parentResourceId = parentProp.GetString();

                if (resourceType.Equals("microsoft.insights/components", StringComparison.OrdinalIgnoreCase)
                    && props.TryGetProperty("AppId", out JsonElement appIdProp))
                    appInsightsAppId = appIdProp.GetString();

                propertyRefs = ExtractPropertyReferences(props, resourceId);
            }

            if (!string.IsNullOrWhiteSpace(resourceId))
                results.Add(new AzureResourceRecord(resourceId, resourceType, name, parentResourceId, appInsightsAppId, propertyRefs, resourceGroup, tags));
        }

        return results;
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
        int rgIndex = Array.IndexOf(columnNames, "resourceGroup");
        int tagsIndex = Array.IndexOf(columnNames, "tags");

        if (idIndex < 0 || typeIndex < 0 || nameIndex < 0)
            return [];

        List<AzureResourceRecord> results = [];
        foreach (JsonElement row in rows.EnumerateArray())
        {
            JsonElement[] cells = row.EnumerateArray().ToArray();
            string resourceId = cells[idIndex].GetString() ?? string.Empty;
            string resourceType = cells[typeIndex].GetString() ?? string.Empty;
            string name = cells[nameIndex].GetString() ?? string.Empty;
            string? resourceGroup = rgIndex >= 0 && cells[rgIndex].ValueKind == JsonValueKind.String
                ? cells[rgIndex].GetString()
                : null;
            IReadOnlyDictionary<string, string>? tags = tagsIndex >= 0
                ? ParseTags(cells[tagsIndex])
                : null;

            string? parentResourceId = null;
            string? appInsightsAppId = null;
            IReadOnlyCollection<string> propertyRefs = Array.Empty<string>();
            if (propsIndex >= 0 && cells[propsIndex].ValueKind == JsonValueKind.Object)
            {
                if (cells[propsIndex].TryGetProperty("parentResourceId", out JsonElement parentProp))
                    parentResourceId = parentProp.GetString();

                if (resourceType.Equals("microsoft.insights/components", StringComparison.OrdinalIgnoreCase)
                    && cells[propsIndex].TryGetProperty("AppId", out JsonElement appIdProp))
                    appInsightsAppId = appIdProp.GetString();

                propertyRefs = ExtractPropertyReferences(cells[propsIndex], resourceId);
            }

            if (!string.IsNullOrWhiteSpace(resourceId))
                results.Add(new AzureResourceRecord(resourceId, resourceType, name, parentResourceId, appInsightsAppId, propertyRefs, resourceGroup, tags));
        }

        return results;
    }

    private static string ExtractAzureErrorDetail(string responseJson)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("error", out JsonElement errorElement))
            {
                string? code = errorElement.TryGetProperty("code", out JsonElement c) ? c.GetString() : null;
                string? message = errorElement.TryGetProperty("message", out JsonElement m) ? m.GetString() : null;
                if (code is not null || message is not null)
                    return $"{code}: {message}";
            }
        }
        catch
        {
        }

        return responseJson.Length > 300 ? responseJson[..300] : responseJson;
    }

    private static IReadOnlyCollection<string> ExtractPropertyReferences(JsonElement props, string selfResourceId)
    {
        var collected = new List<string>();
        CollectArmReferences(props, selfResourceId.ToLowerInvariant(), collected);
        return collected;
    }

    private static void CollectArmReferences(JsonElement element, string selfResourceIdLower, List<string> collected)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                    CollectArmReferences(property.Value, selfResourceIdLower, collected);
                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                    CollectArmReferences(item, selfResourceIdLower, collected);
                break;

            case JsonValueKind.String:
                string? value = element.GetString();
                if (value is not null && IsArmResourceId(value) && !value.Equals(selfResourceIdLower, StringComparison.OrdinalIgnoreCase))
                    collected.Add(value);
                break;
        }
    }

    private static bool IsArmResourceId(string value)
    {
        var lower = value.AsSpan();
        bool hasSubscriptions = lower.Contains("/subscriptions/", StringComparison.OrdinalIgnoreCase);
        if (!hasSubscriptions) return false;

        bool hasProviders = lower.Contains("/providers/", StringComparison.OrdinalIgnoreCase);
        bool hasResourceGroups = lower.Contains("/resourcegroups/", StringComparison.OrdinalIgnoreCase);
        return hasProviders || hasResourceGroups;
    }

    private static IReadOnlyDictionary<string, string>? ParseTags(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        Dictionary<string, string> tags = new(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                var value = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    tags[property.Name] = value;
            }
        }

        return tags.Count == 0 ? null : tags;
    }

    private sealed record ResourceGraphRequest(
        [property: JsonPropertyName("subscriptions")] string[] Subscriptions,
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("options")] ResourceGraphOptions Options);

    private sealed record ResourceGraphOptions(
        [property: JsonPropertyName("$top")] int Top,
        [property: JsonPropertyName("$skipToken"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SkipToken);

    private sealed record ResourceGraphPage(
        IReadOnlyCollection<AzureResourceRecord> Records,
        string? SkipToken);

    private sealed record ClientCredentialsTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
}
