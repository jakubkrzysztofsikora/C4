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
        IQueryable<double> query = dbContext.Metrics
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId && m.Service == service)
            .Select(m => m.Value);

        bool hasAny = await query.AnyAsync(cancellationToken);
        if (!hasAny)
        {
            return null;
        }

        double avg = await query.AverageAsync(cancellationToken);
        var status = avg >= .8 ? ServiceHealthStatus.Green : avg >= .5 ? ServiceHealthStatus.Yellow : ServiceHealthStatus.Red;
        return new ServiceHealth(projectId, service, avg, status, DateTime.UtcNow);
    }

    public async Task<IReadOnlyCollection<ServiceHealth>> GetAllServiceHealthAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var grouped = await dbContext.Metrics
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .GroupBy(m => m.Service)
            .Select(g => new { Service = g.Key, Avg = g.Average(m => m.Value) })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g =>
            {
                var status = g.Avg >= .8 ? ServiceHealthStatus.Green : g.Avg >= .5 ? ServiceHealthStatus.Yellow : ServiceHealthStatus.Red;
                return new ServiceHealth(projectId, g.Service, g.Avg, status, DateTime.UtcNow);
            })
            .ToArray();
    }
}
