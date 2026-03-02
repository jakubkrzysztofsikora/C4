namespace C4.Modules.Telemetry.Application.Ports;

public interface IApplicationInsightsClient
{
    Task<IReadOnlyCollection<ApplicationInsightsHealthRecord>> QueryServiceHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ApplicationInsightsDependencyRecord>> QueryDependencyHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken);
}

public sealed record ApplicationInsightsHealthRecord(
    string Service,
    double Score,
    DateTime ObservedAtUtc,
    double? RequestRate = null,
    double? ErrorRate = null,
    double? P95LatencyMs = null);

public sealed record ApplicationInsightsDependencyRecord(
    string SourceService,
    string TargetService,
    double RequestRate,
    double ErrorRate,
    double P95LatencyMs,
    DateTime ObservedAtUtc,
    string? Protocol = null);
