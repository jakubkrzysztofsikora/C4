using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Resources;

public sealed class DiscoveredResource : Entity<DiscoveredResourceId>
{
    private DiscoveredResource(DiscoveredResourceId id, string resourceId, string resourceType, string name) : base(id)
    {
        ResourceId = resourceId;
        ResourceType = resourceType;
        Name = name;
    }

    public string ResourceId { get; }

    public string ResourceType { get; }

    public string Name { get; }

    public static DiscoveredResource Create(string resourceId, string resourceType, string name) =>
        new(DiscoveredResourceId.New(), resourceId.Trim(), resourceType.Trim(), name.Trim());
}
