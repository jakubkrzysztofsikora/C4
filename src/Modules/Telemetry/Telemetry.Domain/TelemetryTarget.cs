namespace C4.Modules.Telemetry.Domain;

public sealed record TelemetryTarget(
    string Id,
    TelemetryProvider Provider,
    AuthMode AuthMode,
    IReadOnlyDictionary<string, string> ConnectionMetadata);
