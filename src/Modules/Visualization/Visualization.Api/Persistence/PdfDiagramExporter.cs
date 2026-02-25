using C4.Modules.Visualization.Application.Ports;
using System.Text;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class PdfDiagramExporter : IDiagramExporter
{
    public byte[] Export(string diagramJson)
    {
        return Encoding.UTF8.GetBytes($"PDF-FAKE\n{diagramJson}");
    }
}
