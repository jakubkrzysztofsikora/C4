namespace C4.Modules.Discovery.Application.Ports;

public interface IDiscoveryTelemetryEventSink
{
    Task EmitAsync(DiscoveryTelemetryEvent telemetryEvent, CancellationToken cancellationToken);
}

public sealed record DiscoveryTelemetryEvent(
    string EventName,
    IReadOnlyDictionary<string, object?> Payload,
    DateTime OccurredAtUtc);

