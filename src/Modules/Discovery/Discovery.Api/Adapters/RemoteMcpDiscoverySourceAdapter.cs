using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class RemoteMcpDiscoverySourceAdapter(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<RemoteMcpDiscoverySourceAdapter> logger) : IDiscoverySourceAdapter
{
    public DiscoverySourceKind Source => DiscoverySourceKind.RemoteMcp;

    public async Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Domain.McpServers.McpServerConfig> configs = await LoadConfigsForProjectAsync(request.ProjectId, cancellationToken);

        if (configs.Count == 0)
            return Array.Empty<DiscoveryResourceDescriptor>();

        List<DiscoveryResourceDescriptor> results = [];

        foreach (Domain.McpServers.McpServerConfig config in configs)
        {
            IReadOnlyCollection<DiscoveryResourceDescriptor> discovered = await DiscoverFromServerAsync(config, cancellationToken);
            results.AddRange(discovered);
        }

        return results;
    }

    private async Task<IReadOnlyCollection<Domain.McpServers.McpServerConfig>> LoadConfigsForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IMcpServerConfigRepository repository = scope.ServiceProvider.GetRequiredService<IMcpServerConfigRepository>();
        return await repository.GetByProjectIdAsync(projectId, cancellationToken);
    }

    private async Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> DiscoverFromServerAsync(Domain.McpServers.McpServerConfig config, CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            string requestJson = JsonSerializer.Serialize(new McpDiscoverRequest(config.ProjectId.ToString()));
            StringContent content = new(requestJson, Encoding.UTF8, "application/json");

            string url = $"{config.Endpoint.TrimEnd('/')}/tools/discover";
            HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("MCP server {ServerName} at {Endpoint} returned {StatusCode}", config.Name, config.Endpoint, response.StatusCode);
                return Array.Empty<DiscoveryResourceDescriptor>();
            }

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseMcpResponse(responseJson, config.Name);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException { InnerException: TimeoutException })
        {
            logger.LogWarning(ex, "MCP server {ServerName} at {Endpoint} is unreachable", config.Name, config.Endpoint);
            return Array.Empty<DiscoveryResourceDescriptor>();
        }
    }

    private IReadOnlyCollection<DiscoveryResourceDescriptor> ParseMcpResponse(string responseJson, string serverName)
    {
        try
        {
            McpResourceItem[]? items = JsonSerializer.Deserialize<McpResourceItem[]>(responseJson, JsonOptions);

            if (items is null)
                return Array.Empty<DiscoveryResourceDescriptor>();

            return items
                .Where(item => !string.IsNullOrWhiteSpace(item.ResourceId))
                .Select(item => new DiscoveryResourceDescriptor(
                    item.ResourceId,
                    item.ResourceType ?? "unknown",
                    item.Name ?? item.ResourceId,
                    item.ParentResourceId,
                    Source))
                .ToArray();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse MCP response from server {ServerName}", serverName);
            return Array.Empty<DiscoveryResourceDescriptor>();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record McpDiscoverRequest([property: JsonPropertyName("subscriptionId")] string SubscriptionId);

    private sealed record McpResourceItem(
        [property: JsonPropertyName("resourceId")] string ResourceId,
        [property: JsonPropertyName("resourceType")] string? ResourceType,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("parentResourceId")] string? ParentResourceId);
}
