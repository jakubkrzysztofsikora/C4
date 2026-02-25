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

    public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken)
    {
        _graphs[graph.ProjectId] = graph;
        return Task.CompletedTask;
    }
}
