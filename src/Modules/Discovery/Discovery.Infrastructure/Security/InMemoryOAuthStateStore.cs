using System.Collections.Concurrent;
using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Infrastructure.Security;

public sealed class InMemoryOAuthStateStore : IOAuthStateStore
{
    private static readonly TimeSpan StateExpiration = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _states = new();

    public Task StoreAsync(string state, CancellationToken cancellationToken)
    {
        PurgeExpired();
        _states[state] = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task<bool> ValidateAndConsumeAsync(string state, CancellationToken cancellationToken)
    {
        if (!_states.TryRemove(state, out var createdAt))
            return Task.FromResult(false);

        bool isValid = DateTimeOffset.UtcNow - createdAt < StateExpiration;
        return Task.FromResult(isValid);
    }

    private void PurgeExpired()
    {
        var threshold = DateTimeOffset.UtcNow - StateExpiration;
        foreach (var kvp in _states)
        {
            if (kvp.Value < threshold)
                _states.TryRemove(kvp.Key, out _);
        }
    }
}
