using MediatR;

namespace C4.Modules.Telemetry.Application.IntegrationEvents;

public sealed record TelemetryUpdatedIntegrationEvent(Guid ProjectId, IReadOnlyCollection<TelemetryUpdatedServiceItem> Services) : INotification;

public sealed record TelemetryUpdatedServiceItem(string Service, double Score, string Status);
