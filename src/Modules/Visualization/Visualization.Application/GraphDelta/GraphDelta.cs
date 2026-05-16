namespace C4.Modules.Visualization.Application.GraphDelta;

public sealed record GraphDelta(
    IReadOnlyCollection<GraphDeltaNode> AddedNodes,
    IReadOnlyCollection<string> RemovedNodeIds,
    IReadOnlyCollection<GraphDeltaNode> UpdatedNodes,
    IReadOnlyCollection<GraphDeltaEdge> AddedEdges,
    IReadOnlyCollection<string> RemovedEdgeIds);
