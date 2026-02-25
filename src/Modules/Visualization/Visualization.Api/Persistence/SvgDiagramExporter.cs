using C4.Modules.Visualization.Application.Ports;
using System.Text;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class SvgDiagramExporter : IDiagramExporter
{
    public string Format => "svg";

    public Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken)
    {
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='800' height='400'><text x='20' y='40'>Diagram</text><text x='20' y='70'>{System.Security.SecurityElement.Escape(diagramJson)}</text></svg>";
        return Task.FromResult(Encoding.UTF8.GetBytes(svg));
    }
}
