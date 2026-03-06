using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Discovery.Infrastructure.Services;

public sealed class InMemoryDriftQueryService : IDriftQueryService
{
    public Task<IReadOnlyCollection<string>> GetDriftedResourceIdsAsync(
        IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<string>>([]);

    public Task<DriftRunRecord?> GetLatestRunAsync(Guid projectId, CancellationToken cancellationToken)
        => Task.FromResult<DriftRunRecord?>(null);
}
