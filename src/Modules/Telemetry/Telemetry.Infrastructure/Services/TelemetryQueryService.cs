using C4.Modules.Telemetry.Application.Ports;
using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Telemetry.Infrastructure.Services;

public sealed class TelemetryQueryService(
    ITelemetryRepository repository,
    IApplicationInsightsClient applicationInsightsClient) : ITelemetryQueryService
{
    public async Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var merged = new Dictionary<string, ServiceHealthSummary>(StringComparer.OrdinalIgnoreCase);

        var healthRecords = await repository.GetAllServiceHealthAsync(projectId, cancellationToken);
        foreach (var h in healthRecords)
        {
            merged[h.Service] = new ServiceHealthSummary(
                h.Service,
                h.Score,
                h.Status.ToString(),
                RequestRate: null,
                ErrorRate: null,
                P95LatencyMs: null,
                TelemetryStatus: "known");
        }

        var appInsightsRecords = await applicationInsightsClient.QueryServiceHealthAsync(projectId, TimeSpan.FromMinutes(30), cancellationToken);
        foreach (var record in appInsightsRecords)
        {
            merged[record.Service] = new ServiceHealthSummary(
                record.Service,
                Math.Clamp(record.Score, 0, 1),
                ResolveStatus(record.Score),
                RequestRate: record.RequestRate,
                ErrorRate: record.ErrorRate,
                P95LatencyMs: record.P95LatencyMs,
                TelemetryStatus: "known");
        }

        return merged.Values.ToArray();
    }

    public async Task<IReadOnlyCollection<ServiceDependencySummary>> GetDependencySummariesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var appInsightsRecords = await applicationInsightsClient.QueryDependencyHealthAsync(
            projectId,
            TimeSpan.FromMinutes(30),
            cancellationToken);

        return appInsightsRecords
            .Where(record =>
                !string.IsNullOrWhiteSpace(record.SourceService)
                && !string.IsNullOrWhiteSpace(record.TargetService))
            .Select(record => new ServiceDependencySummary(
                record.SourceService,
                record.TargetService,
                record.RequestRate,
                record.ErrorRate,
                record.P95LatencyMs,
                record.Protocol,
                TelemetryStatus: "known"))
            .ToArray();
    }

    private static string ResolveStatus(double score)
    {
        var normalized = Math.Clamp(score, 0, 1);
        if (normalized >= 0.8) return "green";
        if (normalized >= 0.5) return "yellow";
        return "red";
    }
}
