namespace C4.Shared.Kernel.Contracts;

public interface ITelemetryQueryService
{
    Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ServiceDependencySummary>> GetDependencySummariesAsync(Guid projectId, CancellationToken cancellationToken);
}

public sealed record ServiceHealthSummary(
    string Service,
    double Score,
    string Status,
    double? RequestRate = null,
    double? ErrorRate = null,
    double? P95LatencyMs = null,
    string TelemetryStatus = "known");

public sealed record ServiceDependencySummary(
    string SourceService,
    string TargetService,
    double RequestRate,
    double ErrorRate,
    double P95LatencyMs,
    string? Protocol = null,
    string TelemetryStatus = "known");
