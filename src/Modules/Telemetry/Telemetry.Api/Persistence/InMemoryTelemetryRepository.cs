using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;

namespace C4.Modules.Telemetry.Api.Persistence;

public sealed class InMemoryTelemetryRepository : ITelemetryRepository
{
    private readonly List<MetricDataPoint> _metrics = [];

    public Task AddMetricAsync(MetricDataPoint metric, CancellationToken cancellationToken)
    {
        _metrics.Add(metric);
        return Task.CompletedTask;
    }

    public Task<ServiceHealth?> GetServiceHealthAsync(Guid projectId, string service, CancellationToken cancellationToken)
    {
        var points = _metrics.Where(m => m.ProjectId == projectId && m.Service == service).ToArray();
        if (points.Length == 0) return Task.FromResult<ServiceHealth?>(null);
        var avg = points.Average(p => p.Value);
        var status = avg >= .8 ? ServiceHealthStatus.Green : avg >= .5 ? ServiceHealthStatus.Yellow : ServiceHealthStatus.Red;
        return Task.FromResult<ServiceHealth?>(new ServiceHealth(projectId, service, avg, status, DateTime.UtcNow));
    }

    public Task<IReadOnlyCollection<ServiceHealth>> GetAllServiceHealthAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var projectMetrics = _metrics.Where(m => m.ProjectId == projectId).ToArray();
        var results = projectMetrics
            .GroupBy(m => m.Service)
            .Select(g =>
            {
                var avg = g.Average(p => p.Value);
                var status = avg >= .8 ? ServiceHealthStatus.Green : avg >= .5 ? ServiceHealthStatus.Yellow : ServiceHealthStatus.Red;
                return new ServiceHealth(projectId, g.Key, avg, status, DateTime.UtcNow);
            })
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<ServiceHealth>>(results);
    }
}
