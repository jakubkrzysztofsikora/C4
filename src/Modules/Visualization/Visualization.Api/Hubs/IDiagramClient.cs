namespace C4.Modules.Visualization.Api.Hubs;

public interface IDiagramClient
{
    Task DiagramUpdated(Guid projectId, string diagramJson, CancellationToken cancellationToken);
    Task HealthOverlayChanged(Guid projectId, string healthJson, CancellationToken cancellationToken);
    Task GraphDelta(Guid projectId, string deltaJson, CancellationToken cancellationToken);
}
