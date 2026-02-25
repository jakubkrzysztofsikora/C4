using C4.Modules.Telemetry.Domain.Metrics;

namespace C4.Modules.Telemetry.Application.Ports;

public interface ITelemetryRepository
{
    Task AddMetricAsync(MetricDataPoint metric, CancellationToken cancellationToken);
    Task<ServiceHealth?> GetServiceHealthAsync(Guid projectId, string service, CancellationToken cancellationToken);
}
