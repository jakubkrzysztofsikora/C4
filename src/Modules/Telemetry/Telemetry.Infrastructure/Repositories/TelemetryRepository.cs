using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Telemetry.Infrastructure.Repositories;

public sealed class TelemetryRepository(TelemetryDbContext dbContext) : ITelemetryRepository
{
    public async Task AddMetricAsync(MetricDataPoint metric, CancellationToken cancellationToken)
    {
        await dbContext.Metrics.AddAsync(new TelemetryMetricEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = metric.ProjectId,
            Service = metric.Service,
            Value = metric.Value,
            TimestampUtc = metric.TimestampUtc
        }, cancellationToken);
    }

    public async Task<ServiceHealth?> GetServiceHealthAsync(Guid projectId, string service, CancellationToken cancellationToken)
    {
        var points = await dbContext.Metrics
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId && m.Service == service)
            .Select(m => m.Value)
            .ToArrayAsync(cancellationToken);

        if (points.Length == 0)
        {
            return null;
        }

        var avg = points.Average();
        var status = avg >= .8 ? ServiceHealthStatus.Green : avg >= .5 ? ServiceHealthStatus.Yellow : ServiceHealthStatus.Red;
        return new ServiceHealth(projectId, service, avg, status, DateTime.UtcNow);
    }
}
