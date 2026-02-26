using C4.Modules.Telemetry.Application.Ports;
using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Telemetry.Infrastructure.Services;

public sealed class TelemetryQueryService(ITelemetryRepository repository) : ITelemetryQueryService
{
    public async Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var healthRecords = await repository.GetAllServiceHealthAsync(projectId, cancellationToken);
        return healthRecords
            .Select(h => new ServiceHealthSummary(h.Service, h.Score, h.Status.ToString()))
            .ToArray();
    }
}
