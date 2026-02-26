using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class DiscoveryStructuredTelemetryEventSink(ILogger<DiscoveryStructuredTelemetryEventSink> logger)
    : IDiscoveryTelemetryEventSink
{
    public Task EmitAsync(DiscoveryTelemetryEvent telemetryEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Discovery telemetry event {EventName} {@Payload}", telemetryEvent.EventName, telemetryEvent.Payload);
        return Task.CompletedTask;
    }
}

