using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Relationships;

public sealed class ResourceRelationship : Entity<ResourceRelationshipId>
{
    private ResourceRelationship(ResourceRelationshipId id, DiscoveredResourceId source, DiscoveredResourceId target, string type) : base(id)
    {
        SourceResourceId = source;
        TargetResourceId = target;
        RelationshipType = type;
    }

    public DiscoveredResourceId SourceResourceId { get; }

    public DiscoveredResourceId TargetResourceId { get; }

    public string RelationshipType { get; }

    public static ResourceRelationship Create(DiscoveredResourceId source, DiscoveredResourceId target, string type) =>
        new(ResourceRelationshipId.New(), source, target, type.Trim());
}
