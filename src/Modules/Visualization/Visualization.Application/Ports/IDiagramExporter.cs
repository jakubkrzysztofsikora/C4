namespace C4.Modules.Visualization.Application.Ports;

public interface IDiagramExporter
{
    byte[] Export(string diagramJson);
}
