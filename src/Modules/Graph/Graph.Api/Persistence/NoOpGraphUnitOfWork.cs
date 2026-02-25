using C4.Shared.Kernel;

namespace C4.Modules.Graph.Api.Persistence;

public sealed class NoOpGraphUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
