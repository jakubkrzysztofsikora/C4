using C4.Modules.Graph.Domain.ArchitectureGraph;

namespace C4.Modules.Graph.Application.Ports;

public interface IArchitectureGraphRepository
{
    Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task<ArchitectureGraph?> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken);
    Task<GraphDataProjection> GetProjectionByProjectIdAsync(Guid projectId, Guid? snapshotId, CancellationToken cancellationToken);
    Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken);
    Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken);
}
