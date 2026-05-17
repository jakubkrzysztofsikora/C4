using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;

namespace C4.Modules.Graph.Api.Persistence;

public sealed class InMemoryArchitectureGraphRepository : IArchitectureGraphRepository
{
    private readonly Dictionary<Guid, ArchitectureGraph> _graphs = [];

    public Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        _graphs.TryGetValue(projectId, out var graph);
        return Task.FromResult(graph);
    }

    public Task<ArchitectureGraph?> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken) =>
        GetByProjectIdAsync(projectId, cancellationToken);

    public Task<GraphDataProjection> GetProjectionByProjectIdAsync(
        Guid projectId,
        Guid? snapshotId,
        CancellationToken cancellationToken)
    {
        if (!_graphs.TryGetValue(projectId, out var graph))
            return Task.FromResult(new GraphDataProjection(false, [], [], null));

        var nodes = graph.Nodes
            .Select(n => new ProjectedNode(
                n.Id.Value,
                n.ExternalResourceId,
                n.Name,
                (int)n.Level,
                n.ParentId?.Value,
                n.Properties.Technology,
                n.Properties.Domain,
                n.Properties.IsInfrastructure,
                n.Properties.ClassificationSource,
                n.Properties.ClassificationConfidence,
                n.Properties.Tags))
            .ToList();

        var edges = graph.Edges
            .Select(e => new ProjectedEdge(
                e.Id.Value,
                e.SourceNodeId.Value,
                e.TargetNodeId.Value,
                e.Properties.Protocol))
            .ToList();

        ProjectedSnapshot? snapshot = null;
        if (snapshotId.HasValue)
        {
            var match = graph.Snapshots.FirstOrDefault(s => s.Id.Value == snapshotId.Value);
            if (match is not null)
                snapshot = new ProjectedSnapshot(match.Id.Value, match.NodesJson, match.EdgesJson);
        }

        return Task.FromResult(new GraphDataProjection(true, nodes, edges, snapshot));
    }

    public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken)
    {
        _graphs[graph.ProjectId] = graph;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken)
    {
        _graphs.Remove(graph.ProjectId);
        return Task.CompletedTask;
    }
}
