using C4.Modules.Graph.Domain.GraphNode;
using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.GraphEdge;

public sealed class GraphEdge : Entity<GraphEdgeId>
{
#pragma warning disable CS8618
    private GraphEdge() : base(default!) { }
#pragma warning restore CS8618

    private GraphEdge(GraphEdgeId id, GraphNodeId sourceNodeId, GraphNodeId targetNodeId, EdgeProperties properties) : base(id)
    {
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        Properties = properties;
    }

    public GraphNodeId SourceNodeId { get; }
    public GraphNodeId TargetNodeId { get; }
    public EdgeProperties Properties { get; }

    public static GraphEdge Create(GraphNodeId sourceNodeId, GraphNodeId targetNodeId, EdgeProperties properties) =>
        new(GraphEdgeId.New(), sourceNodeId, targetNodeId, properties);
}
