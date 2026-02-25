namespace C4.Modules.Visualization.Application.Ports;

public interface IDiagramNotifier
{
    Task NotifyDiagramUpdatedAsync(Guid projectId, string diagramJson, CancellationToken cancellationToken);
}
