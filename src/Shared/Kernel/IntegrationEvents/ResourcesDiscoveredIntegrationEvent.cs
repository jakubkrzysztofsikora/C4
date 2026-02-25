using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record ResourcesDiscoveredIntegrationEvent(Guid ProjectId, IReadOnlyCollection<DiscoveredResourceEventItem> Resources) : INotification;

public sealed record DiscoveredResourceEventItem(string ResourceId, string ResourceType, string Name);
