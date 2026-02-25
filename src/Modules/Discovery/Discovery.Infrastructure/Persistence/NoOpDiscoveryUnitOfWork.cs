using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Infrastructure.Persistence;

public sealed class NoOpDiscoveryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
