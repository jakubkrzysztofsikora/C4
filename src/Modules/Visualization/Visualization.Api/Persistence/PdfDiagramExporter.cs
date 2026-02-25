using C4.Modules.Visualization.Application.Ports;
using System.Text;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class PdfDiagramExporter : IDiagramExporter
{
    public string Format => "pdf";

    public Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken)
    {
        return Task.FromResult(Encoding.UTF8.GetBytes($"PDF-FAKE\n{diagramJson}"));
    }
}
