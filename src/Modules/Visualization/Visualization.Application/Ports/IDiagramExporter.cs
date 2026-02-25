namespace C4.Modules.Visualization.Application.Ports;

public interface IDiagramExporter
{
    string Format { get; }
    Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken);
}
