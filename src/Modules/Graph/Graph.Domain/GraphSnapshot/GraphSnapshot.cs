namespace C4.Modules.Graph.Domain.GraphSnapshot;

public sealed record GraphSnapshot(GraphSnapshotId Id, DateTime CreatedAtUtc)
{
    public IReadOnlyCollection<C4.Modules.Graph.Domain.GraphNode.GraphNode> Nodes { get; init; } = Array.Empty<C4.Modules.Graph.Domain.GraphNode.GraphNode>();
    public IReadOnlyCollection<C4.Modules.Graph.Domain.GraphEdge.GraphEdge> Edges { get; init; } = Array.Empty<C4.Modules.Graph.Domain.GraphEdge.GraphEdge>();

    public static GraphSnapshot From(
        IReadOnlyCollection<C4.Modules.Graph.Domain.GraphNode.GraphNode> nodes,
        IReadOnlyCollection<C4.Modules.Graph.Domain.GraphEdge.GraphEdge> edges) =>
        new(GraphSnapshotId.New(), DateTime.UtcNow) { Nodes = nodes.ToArray(), Edges = edges.ToArray() };
}
