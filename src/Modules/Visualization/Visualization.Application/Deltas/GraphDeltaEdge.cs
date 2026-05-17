namespace C4.Modules.Visualization.Application.Deltas;

public sealed record GraphDeltaEdge(
    string Id,
    string SourceNodeId,
    string TargetNodeId,
    string? Protocol,
    string? TrafficState);
