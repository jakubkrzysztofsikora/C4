using C4.Shared.Kernel;

namespace C4.Modules.Telemetry.Api.Persistence;

public sealed class NoOpTelemetryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
