using C4.Shared.Kernel;

namespace C4.Modules.Telemetry.Domain.Errors;

public static class TelemetryErrors
{
    public static Error HealthNotFound(Guid projectId, string service) => new("telemetry.health.not_found", $"Health for {projectId}/{service} not found.");
}
