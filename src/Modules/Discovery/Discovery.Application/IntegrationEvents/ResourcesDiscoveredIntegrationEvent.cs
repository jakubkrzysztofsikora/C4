using MediatR;

namespace C4.Modules.Discovery.Application.IntegrationEvents;

public sealed record ResourcesDiscoveredIntegrationEvent(Guid ProjectId, IReadOnlyCollection<DiscoveredResourceEventItem> Resources) : INotification;

public sealed record DiscoveredResourceEventItem(string ResourceId, string ResourceType, string Name);
