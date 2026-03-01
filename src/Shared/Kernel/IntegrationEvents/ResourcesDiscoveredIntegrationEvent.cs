using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record ResourcesDiscoveredIntegrationEvent(Guid ProjectId, IReadOnlyCollection<DiscoveredResourceEventItem> Resources) : INotification;

public sealed record ResourceRelationship(string Type, string TargetResourceId);

public sealed record DiscoveredResourceEventItem(
    string ResourceId,
    string ResourceType,
    string Name,
    string? FriendlyName,
    string? ServiceType,
    string? C4Level,
    bool IncludeInDiagram,
    string? ParentResourceId,
    string SourceProvenance = "azure",
    double ConfidenceScore = 1.0,
    IReadOnlyCollection<ResourceRelationship>? Relationships = null,
    string? StableResourceId = null);
