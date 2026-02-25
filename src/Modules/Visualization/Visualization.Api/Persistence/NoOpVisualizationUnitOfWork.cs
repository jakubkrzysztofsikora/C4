using C4.Shared.Kernel;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class NoOpVisualizationUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
