using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record DriftDetectedIntegrationEvent(Guid SubscriptionId, IReadOnlyCollection<DriftDetectedEventItem> Items) : INotification;

public sealed record DriftDetectedEventItem(string ResourceId, string Status);
