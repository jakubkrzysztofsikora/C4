using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Graph.Infrastructure.Persistence.Repositories;

public sealed class ArchitectureGraphRepository(GraphDbContext dbContext) : IArchitectureGraphRepository
{
    public async Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Graphs
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.Snapshots)
            .FirstOrDefaultAsync(g => g.ProjectId == projectId, cancellationToken);

    public async Task<ArchitectureGraph?> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Graphs
            .AsNoTracking()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.ProjectId == projectId, cancellationToken);

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
