namespace C4.Modules.Telemetry.Domain.Metrics;

public sealed record MetricDataPoint(Guid ProjectId, string Service, double Value, DateTime TimestampUtc);
