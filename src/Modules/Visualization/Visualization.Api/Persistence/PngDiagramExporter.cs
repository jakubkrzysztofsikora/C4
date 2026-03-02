using C4.Modules.Visualization.Application.Ports;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class PngDiagramExporter : IDiagramExporter
{
    public string Format => "png";

    public async Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken)
    {
        var model = DiagramExportParser.Parse(diagramJson);
        var layout = DiagramExportParser.CreateLayout(model);

        using var image = new Image<Rgba32>(layout.Width, layout.Height, new Rgba32(11, 16, 32));
        image.Mutate(context =>
        {
            var edgeColor = new Rgba32(74, 125, 194);
            var nodeFill = new Rgba32(20, 33, 61);
            var nodeBorder = new Rgba32(60, 111, 179);

            foreach (var edge in model.Edges)
            {
                if (!layout.Positions.TryGetValue(edge.SourceNodeId, out var source) || !layout.Positions.TryGetValue(edge.TargetNodeId, out var target))
                    continue;

                var x1 = source.X + layout.NodeWidth;
                var y1 = source.Y + (layout.NodeHeight / 2f);
                var x2 = target.X;
                var y2 = target.Y + (layout.NodeHeight / 2f);
                var mx1 = x1 + 36;
                var mx2 = x2 - 36;

                context.DrawLine(edgeColor, 2f, new PointF(x1, y1), new PointF(mx1, y1), new PointF(mx2, y2), new PointF(x2, y2));
            }

            foreach (var node in model.Nodes)
            {
                if (!layout.Positions.TryGetValue(node.Id, out var position))
                    continue;

                var x = position.X;
                var y = position.Y;
                var rect = new RectangularPolygon(x, y, layout.NodeWidth, layout.NodeHeight);

                context.Fill(nodeFill, rect);
                context.Draw(nodeBorder, 2f, rect);
                context.Fill(new Rgba32(91, 150, 255), new RectangularPolygon(x + 10, y + 10, 8, 8));
                context.Fill(new Rgba32(154, 180, 223), new RectangularPolygon(x + 24, y + 12, 90, 4));
                context.Fill(new Rgba32(154, 180, 223), new RectangularPolygon(x + 24, y + 22, 72, 4));
            }
        });

        await using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
