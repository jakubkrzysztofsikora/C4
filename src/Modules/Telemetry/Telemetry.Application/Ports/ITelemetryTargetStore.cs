using C4.Modules.Telemetry.Domain;

namespace C4.Modules.Telemetry.Application.Ports;

public interface ITelemetryTargetStore
{
    Task<IReadOnlyCollection<TelemetryTarget>> GetTargetsAsync(Guid projectId, CancellationToken cancellationToken);
    Task UpsertTargetAsync(Guid projectId, TelemetryTarget target, CancellationToken cancellationToken);
    Task DeleteTargetAsync(Guid projectId, string targetId, CancellationToken cancellationToken);
}
