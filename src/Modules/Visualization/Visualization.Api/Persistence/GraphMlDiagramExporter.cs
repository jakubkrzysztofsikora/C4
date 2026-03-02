using C4.Modules.Visualization.Application.Ports;
using System.Security;
using System.Text;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class GraphMlDiagramExporter : IDiagramExporter
{
    public string Format => "graphml";

    public Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken)
    {
        var model = DiagramExportParser.Parse(diagramJson);

        StringBuilder builder = new();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        builder.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\">");
        builder.AppendLine("  <key id=\"label\" for=\"node\" attr.name=\"label\" attr.type=\"string\" />");
        builder.AppendLine("  <key id=\"level\" for=\"node\" attr.name=\"level\" attr.type=\"string\" />");
        builder.AppendLine("  <graph id=\"G\" edgedefault=\"directed\">");

        foreach (var node in model.Nodes)
        {
            var label = SecurityElement.Escape(node.Name) ?? node.Name;
            var level = SecurityElement.Escape(node.Level) ?? node.Level;
            builder.AppendLine($"    <node id=\"{node.Id}\"><data key=\"label\">{label}</data><data key=\"level\">{level}</data></node>");
        }

        foreach (var edge in model.Edges)
        {
            builder.AppendLine($"    <edge id=\"{edge.Id}\" source=\"{edge.SourceNodeId}\" target=\"{edge.TargetNodeId}\" />");
        }

        builder.AppendLine("  </graph>");
        builder.AppendLine("</graphml>");

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
    }
}
