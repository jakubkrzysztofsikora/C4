namespace C4.Modules.Telemetry.Domain.Metrics;

public sealed record ServiceHealth(Guid ProjectId, string Service, double Score, ServiceHealthStatus Status, DateTime CalculatedAtUtc);
