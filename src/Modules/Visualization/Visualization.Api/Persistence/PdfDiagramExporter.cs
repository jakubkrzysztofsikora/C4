using C4.Modules.Visualization.Application.Ports;
using System.Text;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class PdfDiagramExporter : IDiagramExporter
{
    public string Format => "pdf";

    public Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken)
    {
        var model = DiagramExportParser.Parse(diagramJson);
        var layout = DiagramExportParser.CreateLayout(model);

        var pageWidth = Math.Max(612, layout.Width);
        var pageHeight = Math.Max(792, layout.Height);

        var content = new StringBuilder();

        // Background
        content.AppendLine("0.043 0.063 0.125 rg");
        content.AppendLine($"0 0 {pageWidth} {pageHeight} re");
        content.AppendLine("f");

        content.AppendLine("1.4 w");
        foreach (var edge in model.Edges)
        {
            if (!layout.Positions.TryGetValue(edge.SourceNodeId, out var source) || !layout.Positions.TryGetValue(edge.TargetNodeId, out var target))
                continue;

            var x1 = source.X + layout.NodeWidth;
            var y1 = ToPdfY(source.Y + (layout.NodeHeight / 2f), pageHeight);
            var x2 = target.X;
            var y2 = ToPdfY(target.Y + (layout.NodeHeight / 2f), pageHeight);
            var cx1 = x1 + 36;
            var cx2 = x2 - 36;

            var (er, eg, eb) = HexToRgb(ExportColorResolver.ResolveEdgeStroke(edge, model.OverlayMode));
            content.AppendLine($"{er:0.###} {eg:0.###} {eb:0.###} RG");
            if (edge.IsDerived)
                content.AppendLine("[5 3] 0 d");
            else
                content.AppendLine("[] 0 d");
            content.AppendLine($"{x1:0.##} {y1:0.##} m");
            content.AppendLine($"{cx1:0.##} {y1:0.##} {cx2:0.##} {y2:0.##} {x2:0.##} {y2:0.##} c");
            content.AppendLine("S");
        }

        // Nodes
        foreach (var node in model.Nodes)
        {
            if (!layout.Positions.TryGetValue(node.Id, out var position))
                continue;

            var x = position.X;
            var y = ToPdfY(position.Y + layout.NodeHeight, pageHeight);

            // Fill
            content.AppendLine("0.078 0.129 0.239 rg");
            content.AppendLine($"{x:0.##} {y:0.##} {layout.NodeWidth} {layout.NodeHeight} re");
            content.AppendLine("f");

            var (br, bg, bb) = HexToRgb(ExportColorResolver.ResolveNodeStroke(node, model.OverlayMode));
            content.AppendLine($"{br:0.###} {bg:0.###} {bb:0.###} RG");
            content.AppendLine("1 w");
            content.AppendLine($"{x:0.##} {y:0.##} {layout.NodeWidth} {layout.NodeHeight} re");
            content.AppendLine("S");

            // Label
            var escapedName = EscapePdfText(node.Name.Length > 42 ? $"{node.Name[..39]}..." : node.Name);
            var escapedLevel = EscapePdfText(node.Level);
            var textX = x + 10;
            var textY = y + layout.NodeHeight - 20;

            content.AppendLine("BT");
            content.AppendLine("/F1 10 Tf");
            content.AppendLine("0.945 0.961 1 rg");
            content.AppendLine($"{textX:0.##} {textY:0.##} Td");
            content.AppendLine($"({escapedName}) Tj");
            content.AppendLine("T*");
            content.AppendLine("/F1 8 Tf");
            content.AppendLine("0.620 0.706 0.875 rg");
            content.AppendLine($"({escapedLevel}) Tj");
            content.AppendLine("ET");
        }

        return Task.FromResult(BuildPdf(pageWidth, pageHeight, content.ToString()));
    }

    private static float ToPdfY(float topOriginY, float pageHeight)
        => pageHeight - topOriginY;

    private static byte[] BuildPdf(int pageWidth, int pageHeight, string content)
    {
        byte[] contentBytes = Encoding.ASCII.GetBytes(content);

        List<string> objects =
        [
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            $"3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {contentBytes.Length} >> stream\n{content}endstream\nendobj\n"
        ];

        StringBuilder pdf = new();
        pdf.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(pdf.Length);
            pdf.Append(obj);
        }

        int xrefOffset = pdf.Length;
        pdf.Append($"xref\n0 {objects.Count + 1}\n");
        pdf.Append("0000000000 65535 f \n");
        for (int i = 1; i <= objects.Count; i++)
        {
            pdf.Append($"{offsets[i]:D10} 00000 n \n");
        }

        pdf.Append("trailer << /Size 6 /Root 1 0 R >>\n");
        pdf.Append("startxref\n");
        pdf.Append(xrefOffset);
        pdf.Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static (double R, double G, double B) HexToRgb(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length != 6) return (0.290, 0.490, 0.760);
        var r = Convert.ToInt32(h[..2], 16) / 255.0;
        var g = Convert.ToInt32(h[2..4], 16) / 255.0;
        var b = Convert.ToInt32(h[4..6], 16) / 255.0;
        return (r, g, b);
    }

    private static string EscapePdfText(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
