using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Tests.Fakes;

internal sealed class InMemoryFeedbackUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;
        return Task.FromResult(1);
    }
}
