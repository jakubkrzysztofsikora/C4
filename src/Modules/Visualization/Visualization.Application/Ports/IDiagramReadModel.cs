namespace C4.Modules.Visualization.Application.Ports;

public interface IDiagramReadModel
{
    Task<string?> GetDiagramJsonAsync(Guid projectId, CancellationToken cancellationToken);
}
