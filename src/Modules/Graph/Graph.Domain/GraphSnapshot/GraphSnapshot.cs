using System.Text.Json;

namespace C4.Modules.Graph.Domain.GraphSnapshot;

public sealed record GraphSnapshot(
    GraphSnapshotId Id,
    DateTime CreatedAtUtc,
    string Source,
    string NodesJson,
    string EdgesJson)
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyCollection<GraphSnapshotNode> Nodes
        => JsonSerializer.Deserialize<GraphSnapshotNode[]>(NodesJson, SnapshotJsonOptions) ?? [];

    public IReadOnlyCollection<GraphSnapshotEdge> Edges
        => JsonSerializer.Deserialize<GraphSnapshotEdge[]>(EdgesJson, SnapshotJsonOptions) ?? [];

    public static GraphSnapshot From(
        IReadOnlyCollection<C4.Modules.Graph.Domain.GraphNode.GraphNode> nodes,
        IReadOnlyCollection<C4.Modules.Graph.Domain.GraphEdge.GraphEdge> edges,
        string source = "discovery")
    {
        var nodeData = nodes.Select(n => new GraphSnapshotNode(
            n.Id.Value,
            n.ExternalResourceId,
            n.Name,
            n.Level.ToString(),
            n.ParentId?.Value,
            n.Properties.Technology,
            n.Properties.Domain,
            n.Properties.IsInfrastructure,
            n.Properties.ClassificationSource,
            n.Properties.ClassificationConfidence,
            n.Properties.Tags)).ToArray();

        var edgeData = edges.Select(e => new GraphSnapshotEdge(
            e.Id.Value,
            e.SourceNodeId.Value,
            e.TargetNodeId.Value,
            e.Properties.Protocol,
            e.Properties.Port,
            e.Properties.Direction)).ToArray();

        return new GraphSnapshot(
            GraphSnapshotId.New(),
            DateTime.UtcNow,
            source,
            JsonSerializer.Serialize(nodeData),
            JsonSerializer.Serialize(edgeData));
    }
}

public sealed record GraphSnapshotNode(
    Guid Id,
    string ExternalResourceId,
    string Name,
    string Level,
    Guid? ParentId,
    string ServiceType,
    string Domain,
    bool IsInfrastructure,
    string ClassificationSource,
    double ClassificationConfidence,
    IReadOnlyCollection<string>? Tags = null);

public sealed record GraphSnapshotEdge(
    Guid Id,
    Guid SourceNodeId,
    Guid TargetNodeId,
    string Protocol,
    int Port,
    string Direction);
