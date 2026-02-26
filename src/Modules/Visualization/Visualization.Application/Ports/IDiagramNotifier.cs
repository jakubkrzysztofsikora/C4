namespace C4.Modules.Visualization.Application.Ports;

public interface IDiagramNotifier
{
    Task NotifyDiagramUpdatedAsync(Guid projectId, string diagramJson, CancellationToken cancellationToken);
    Task NotifyHealthOverlayChangedAsync(Guid projectId, string healthJson, CancellationToken cancellationToken);
}
