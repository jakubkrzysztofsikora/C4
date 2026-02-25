using MediatR;

namespace C4.Modules.Discovery.Application.IntegrationEvents;

public sealed record DriftDetectedIntegrationEvent(Guid SubscriptionId, IReadOnlyCollection<DriftDetectedEventItem> Items) : INotification;

public sealed record DriftDetectedEventItem(string ResourceId, string Status);
