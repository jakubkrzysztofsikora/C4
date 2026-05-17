namespace C4.Modules.Graph.Application.Ports;

public sealed record GraphDataProjection(
    bool Exists,
    IReadOnlyCollection<ProjectedNode> Nodes,
    IReadOnlyCollection<ProjectedEdge> Edges,
    ProjectedSnapshot? Snapshot);

public sealed record ProjectedNode(
    Guid Id,
    string ExternalResourceId,
    string Name,
    int LevelValue,
    Guid? ParentId,
    string Technology,
    string Domain,
    bool IsInfrastructure,
    string ClassificationSource,
    double ClassificationConfidence,
    IReadOnlyCollection<string> Tags);

public sealed record ProjectedEdge(
    Guid Id,
    Guid SourceNodeId,
    Guid TargetNodeId,
    string Protocol);

public sealed record ProjectedSnapshot(
    Guid SnapshotId,
    string NodesJson,
    string EdgesJson);
