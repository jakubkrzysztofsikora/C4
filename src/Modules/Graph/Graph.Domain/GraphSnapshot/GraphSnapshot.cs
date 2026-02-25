namespace C4.Modules.Graph.Domain.GraphSnapshot;

public sealed record GraphSnapshot(
    GraphSnapshotId Id,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<C4.Modules.Graph.Domain.GraphNode.GraphNode> Nodes,
    IReadOnlyCollection<C4.Modules.Graph.Domain.GraphEdge.GraphEdge> Edges)
{
    public static GraphSnapshot From(
        IReadOnlyCollection<C4.Modules.Graph.Domain.GraphNode.GraphNode> nodes,
        IReadOnlyCollection<C4.Modules.Graph.Domain.GraphEdge.GraphEdge> edges) =>
        new(GraphSnapshotId.New(), DateTime.UtcNow, nodes.ToArray(), edges.ToArray());
}
