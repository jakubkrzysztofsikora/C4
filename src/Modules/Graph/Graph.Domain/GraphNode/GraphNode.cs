using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.GraphNode;

public sealed class GraphNode : Entity<GraphNodeId>
{
#pragma warning disable CS8618
    private GraphNode() : base(default!) { }
#pragma warning restore CS8618

    private GraphNode(GraphNodeId id, string externalResourceId, string name, C4Level level, GraphNodeId? parentId, NodeProperties properties) : base(id)
    {
        ExternalResourceId = externalResourceId;
        Name = name;
        Level = level;
        ParentId = parentId;
        Properties = properties;
    }

    public string ExternalResourceId { get; }
    public string Name { get; private set; }
    public C4Level Level { get; }
    public GraphNodeId? ParentId { get; private set; }
    public NodeProperties Properties { get; private set; }

    public static GraphNode Create(string externalResourceId, string name, C4Level level, GraphNodeId? parentId, NodeProperties properties) =>
        new(GraphNodeId.New(), externalResourceId.Trim(), name.Trim(), level, parentId, properties);

    public void Update(string name, NodeProperties properties)
    {
        Name = name;
        Properties = properties;
    }

    public void SetParent(GraphNodeId parentId)
    {
        ParentId = parentId;
    }
}
