namespace C4.Shared.Kernel.Contracts;

public interface ITelemetryQueryService
{
    Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(Guid projectId, CancellationToken cancellationToken);
}

public sealed record ServiceHealthSummary(string Service, double Score, string Status);
