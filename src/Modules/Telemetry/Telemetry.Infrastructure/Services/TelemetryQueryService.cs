using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Telemetry.Infrastructure.Services;

public sealed class TelemetryQueryService(
    ITelemetryRepository repository,
    IApplicationInsightsClient applicationInsightsClient,
    ILogger<TelemetryQueryService> logger) : ITelemetryQueryService
{
    public async Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var merged = new Dictionary<string, ServiceHealthSummary>(StringComparer.OrdinalIgnoreCase);

        IReadOnlyCollection<ServiceHealth> healthRecords;
        try
        {
            healthRecords = await repository.GetAllServiceHealthAsync(projectId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read persisted health telemetry for project {ProjectId}", projectId);
            healthRecords = [];
        }

        foreach (var h in healthRecords)
        {
            if (string.IsNullOrWhiteSpace(h.Service))
                continue;

            var score = SanitizeScore(h.Score);
            merged[h.Service] = new ServiceHealthSummary(
                h.Service,
                score,
                ResolveStatus(score),
                RequestRate: null,
                ErrorRate: null,
                P95LatencyMs: null,
                TelemetryStatus: "known");
        }

        IReadOnlyCollection<ApplicationInsightsHealthRecord> appInsightsRecords;
        try
        {
            appInsightsRecords = await applicationInsightsClient.QueryServiceHealthAsync(projectId, TimeSpan.FromMinutes(30), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Application Insights health query failed for project {ProjectId}", projectId);
            appInsightsRecords = [];
        }

        foreach (var record in appInsightsRecords)
        {
            if (string.IsNullOrWhiteSpace(record.Service))
                continue;

            var score = SanitizeScore(record.Score);
            merged[record.Service] = new ServiceHealthSummary(
                record.Service,
                score,
                ResolveStatus(score),
                RequestRate: SanitizeNonNegative(record.RequestRate),
                ErrorRate: SanitizeRate(record.ErrorRate),
                P95LatencyMs: SanitizeNonNegative(record.P95LatencyMs),
                TelemetryStatus: "known");
        }

        return merged.Values.ToArray();
    }

    public async Task<IReadOnlyCollection<ServiceDependencySummary>> GetDependencySummariesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ApplicationInsightsDependencyRecord> appInsightsRecords;
        try
        {
            appInsightsRecords = await applicationInsightsClient.QueryDependencyHealthAsync(
                projectId,
                TimeSpan.FromMinutes(30),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Application Insights dependency query failed for project {ProjectId}", projectId);
            return [];
        }

        return appInsightsRecords
            .Where(record =>
                !string.IsNullOrWhiteSpace(record.SourceService)
                && !string.IsNullOrWhiteSpace(record.TargetService))
            .Select(record => new ServiceDependencySummary(
                record.SourceService,
                record.TargetService,
                SanitizeNonNegative(record.RequestRate) ?? 0,
                SanitizeRate(record.ErrorRate) ?? 0,
                SanitizeNonNegative(record.P95LatencyMs) ?? 0,
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

    private static double SanitizeScore(double value)
    {
        if (!double.IsFinite(value))
            return 0;

        return Math.Clamp(value, 0, 1);
    }

    private static double? SanitizeNonNegative(double? value)
    {
        if (!value.HasValue || !double.IsFinite(value.Value))
            return null;

        return value.Value < 0 ? 0 : value.Value;
    }

    private static double? SanitizeRate(double? value)
    {
        if (!value.HasValue || !double.IsFinite(value.Value))
            return null;

        return Math.Clamp(value.Value, 0, 1);
    }
}
