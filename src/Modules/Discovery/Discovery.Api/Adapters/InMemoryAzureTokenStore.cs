using System.Collections.Concurrent;
using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class InMemoryAzureTokenStore : IAzureTokenStore
{
    private readonly ConcurrentDictionary<string, AzureTokenInfo> _tokens = new();

    public Task StoreAsync(string externalSubscriptionId, AzureTokenInfo tokenInfo, CancellationToken cancellationToken)
    {
        _tokens.AddOrUpdate(externalSubscriptionId, tokenInfo, (_, _) => tokenInfo);
        return Task.CompletedTask;
    }

    public Task<AzureTokenInfo?> GetAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        _tokens.TryGetValue(externalSubscriptionId, out AzureTokenInfo? tokenInfo);
        return Task.FromResult(tokenInfo);
    }
}
