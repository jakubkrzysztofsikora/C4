using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
using C4.Modules.Graph.Domain.GraphNode;
using C4.Modules.Graph.Domain.GraphSnapshot;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Graph.Infrastructure.Persistence.Repositories;

public sealed class ArchitectureGraphRepository(GraphDbContext dbContext) : IArchitectureGraphRepository
{
    public async Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Graphs
            .AsSplitQuery()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.Snapshots)
            .FirstOrDefaultAsync(g => g.ProjectId == projectId, cancellationToken);

    public async Task<ArchitectureGraph?> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Graphs
            .AsNoTracking()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.Snapshots)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.ProjectId == projectId, cancellationToken);

    public async Task<GraphDataProjection> GetProjectionByProjectIdAsync(
        Guid projectId,
        Guid? snapshotId,
        CancellationToken cancellationToken)
    {
        var graphId = await dbContext.Graphs
            .AsNoTracking()
            .Where(g => g.ProjectId == projectId)
            .Select(g => (Guid?)g.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (graphId is null)
            return new GraphDataProjection(false, [], [], null);

        if (snapshotId.HasValue)
        {
            var snapshot = await dbContext.Set<GraphSnapshot>()
                .AsNoTracking()
                .Where(s => EF.Property<Guid>(s, "ArchitectureGraphId") == graphId.Value
                            && s.Id == new GraphSnapshotId(snapshotId.Value))
                .Select(s => new ProjectedSnapshot(s.Id.Value, s.NodesJson, s.EdgesJson))
                .FirstOrDefaultAsync(cancellationToken);

            return new GraphDataProjection(true, [], [], snapshot);
        }

        var nodes = await dbContext.Nodes
            .AsNoTracking()
            .Where(n => EF.Property<Guid>(n, "ArchitectureGraphId") == graphId.Value)
            .Select(n => new ProjectedNode(
                n.Id.Value,
                n.ExternalResourceId,
                n.Name,
                (int)n.Level,
                n.ParentId == null ? (Guid?)null : n.ParentId.Value,
                n.Properties.Technology,
                n.Properties.Domain,
                n.Properties.IsInfrastructure,
                n.Properties.ClassificationSource,
                n.Properties.ClassificationConfidence,
                n.Properties.Tags))
            .ToListAsync(cancellationToken);

        var edges = await dbContext.Edges
            .AsNoTracking()
            .Where(e => EF.Property<Guid>(e, "ArchitectureGraphId") == graphId.Value)
            .Select(e => new ProjectedEdge(
                e.Id.Value,
                e.SourceNodeId.Value,
                e.TargetNodeId.Value,
                e.Properties.Protocol))
            .ToListAsync(cancellationToken);

        return new GraphDataProjection(true, nodes, edges, null);
    }

    public async Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Graphs.FindAsync([graph.Id], cancellationToken);
        if (existing is null)
            await dbContext.Graphs.AddAsync(graph, cancellationToken);
    }

    public Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken)
    {
        dbContext.Graphs.Remove(graph);
        return Task.CompletedTask;
    }
}
