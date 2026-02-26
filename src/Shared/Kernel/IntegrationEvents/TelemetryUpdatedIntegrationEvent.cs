using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record TelemetryUpdatedIntegrationEvent(Guid ProjectId, IReadOnlyCollection<TelemetryUpdatedServiceItem> Services) : INotification;

public sealed record TelemetryUpdatedServiceItem(string Service, double Score, string Status);
