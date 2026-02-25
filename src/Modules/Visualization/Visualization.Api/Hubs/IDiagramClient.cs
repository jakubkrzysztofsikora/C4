namespace C4.Modules.Visualization.Api.Hubs;

public interface IDiagramClient
{
    Task DiagramUpdated(Guid projectId, string diagramJson);
    Task HealthOverlayChanged(Guid projectId, string healthJson);
    Task NodeAdded(Guid projectId, string nodeJson);
    Task NodeRemoved(Guid projectId, string nodeId);
}
