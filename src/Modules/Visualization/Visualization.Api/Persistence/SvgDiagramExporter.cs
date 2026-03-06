using C4.Modules.Visualization.Application.Ports;
using System.Security;
using System.Text;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class SvgDiagramExporter : IDiagramExporter
{
    public string Format => "svg";

    public Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken)
    {
        var model = DiagramExportParser.Parse(diagramJson);
        var layout = DiagramExportParser.CreateLayout(model);
        var nodePositions = layout.Positions;
        var nodeWidth = layout.NodeWidth;
        var nodeHeight = layout.NodeHeight;
        var width = layout.Width;
        var height = layout.Height;

        var legend = ExportColorResolver.BuildLegend(model.OverlayMode);
        if (legend.Length > 0)
            height += 60;

        StringBuilder edgeBuilder = new();
        foreach (var edge in model.Edges)
        {
            if (!nodePositions.TryGetValue(edge.SourceNodeId, out var source) || !nodePositions.TryGetValue(edge.TargetNodeId, out var target))
                continue;

            var x1 = source.X + nodeWidth;
            var y1 = source.Y + (nodeHeight / 2f);
            var x2 = target.X;
            var y2 = target.Y + (nodeHeight / 2f);
            var stroke = ExportColorResolver.ResolveEdgeStroke(edge, model.OverlayMode);
            var dashAttr = edge.IsDerived ? " stroke-dasharray='5,3'" : "";

            edgeBuilder.AppendLine($"<path d='M{x1},{y1} C{x1 + 36},{y1} {x2 - 36},{y2} {x2},{y2}' stroke='{stroke}' fill='none' stroke-width='1.6'{dashAttr} marker-end='url(#arrow)' />");
        }

        StringBuilder nodeBuilder = new();
        foreach (var node in model.Nodes)
        {
            if (!nodePositions.TryGetValue(node.Id, out var position))
                continue;

            var x = position.X;
            var y = position.Y;
            var escapedName = SecurityElement.Escape(node.Name) ?? node.Name;
            var escapedLevel = SecurityElement.Escape(node.Level) ?? node.Level;
            if (escapedName.Length > 42)
                escapedName = $"{escapedName[..39]}...";
            var strokeColor = ExportColorResolver.ResolveNodeStroke(node, model.OverlayMode);
            nodeBuilder.AppendLine($"<g transform='translate({x},{y})'>");
            nodeBuilder.AppendLine($"<rect width='{nodeWidth}' height='{nodeHeight}' rx='10' ry='10' fill='#14213d' stroke='{strokeColor}' stroke-width='1.4' />");
            nodeBuilder.AppendLine($"<text x='14' y='30' font-family='Inter,Arial,sans-serif' font-size='13' fill='#f1f5ff'>{escapedName}</text>");
            nodeBuilder.AppendLine($"<text x='14' y='52' font-family='Inter,Arial,sans-serif' font-size='11' fill='#9fb4df'>{escapedLevel}</text>");
            nodeBuilder.AppendLine("</g>");
        }

        StringBuilder legendBuilder = new();
        if (legend.Length > 0)
        {
            var legendY = height - 50;
            var legendX = width - (legend.Length * 120) - 20;
            legendBuilder.AppendLine($"<g transform='translate({Math.Max(20, legendX)},{legendY})'>");
            for (var i = 0; i < legend.Length; i++)
            {
                var lx = i * 120;
                legendBuilder.AppendLine($"<rect x='{lx}' y='0' width='14' height='14' rx='3' fill='{legend[i].Color}' />");
                legendBuilder.AppendLine($"<text x='{lx + 20}' y='12' font-family='Inter,Arial,sans-serif' font-size='11' fill='#9fb4df'>{SecurityElement.Escape(legend[i].Label)}</text>");
            }
            legendBuilder.AppendLine("</g>");
        }

        string svg = $"""
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>
  <defs>
    <marker id='arrow' markerWidth='8' markerHeight='8' refX='7' refY='3' orient='auto' markerUnits='strokeWidth'>
      <path d='M0,0 L0,6 L8,3 z' fill='#4a7dc2' />
    </marker>
  </defs>
  <rect width='{width}' height='{height}' fill='#0b1020' />
  {edgeBuilder}
  {nodeBuilder}
  {legendBuilder}
</svg>
""";

        return Task.FromResult(Encoding.UTF8.GetBytes(svg));
    }
}
