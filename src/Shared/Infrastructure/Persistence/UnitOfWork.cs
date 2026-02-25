using C4.Shared.Kernel;

namespace C4.Shared.Infrastructure.Persistence;

public sealed class UnitOfWork(BaseDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
