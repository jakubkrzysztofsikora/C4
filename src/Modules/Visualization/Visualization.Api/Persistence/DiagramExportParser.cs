using System.Text.Json;

namespace C4.Modules.Visualization.Api.Persistence;

internal sealed record DiagramExportNode(string Id, string Name, string Level, float? X = null, float? Y = null);
internal sealed record DiagramExportEdge(string Id, string SourceNodeId, string TargetNodeId);
internal sealed record DiagramExportModel(IReadOnlyCollection<DiagramExportNode> Nodes, IReadOnlyCollection<DiagramExportEdge> Edges);
internal readonly record struct DiagramPosition(float X, float Y);
internal sealed record DiagramExportLayout(
    int Width,
    int Height,
    int NodeWidth,
    int NodeHeight,
    int Padding,
    IReadOnlyDictionary<string, DiagramPosition> Positions);

internal static class DiagramExportParser
{
    private const int MaxCanvasDimension = 4096;

    public static DiagramExportModel Parse(string diagramJson)
    {
        using JsonDocument doc = JsonDocument.Parse(diagramJson);

        var nodes = new List<DiagramExportNode>();
        if (doc.RootElement.TryGetProperty("nodes", out var nodesElement) && nodesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var node in nodesElement.EnumerateArray())
            {
                var id = node.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var name = node.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString()
                    : node.TryGetProperty("label", out var labelElement)
                        ? labelElement.GetString()
                        : null;
                var level = node.TryGetProperty("level", out var levelElement) ? levelElement.GetString() : null;
                var x = TryReadNodeCoordinate(node, "x");
                var y = TryReadNodeCoordinate(node, "y");

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                    continue;

                nodes.Add(new DiagramExportNode(
                    id,
                    name,
                    string.IsNullOrWhiteSpace(level) ? "Container" : level!,
                    x,
                    y));
            }
        }

        var edges = new List<DiagramExportEdge>();
        if (doc.RootElement.TryGetProperty("edges", out var edgesElement) && edgesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var edge in edgesElement.EnumerateArray())
            {
                var id = edge.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var source = edge.TryGetProperty("sourceNodeId", out var sourceElement)
                    ? sourceElement.GetString()
                    : edge.TryGetProperty("from", out var fromElement)
                        ? fromElement.GetString()
                        : null;
                var target = edge.TryGetProperty("targetNodeId", out var targetElement)
                    ? targetElement.GetString()
                    : edge.TryGetProperty("to", out var toElement)
                        ? toElement.GetString()
                        : null;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                    continue;

                edges.Add(new DiagramExportEdge(id, source, target));
            }
        }

        return new DiagramExportModel(nodes, edges);
    }

    public static DiagramExportLayout CreateLayout(DiagramExportModel model)
    {
        const int nodeWidth = 260;
        const int nodeHeight = 74;
        const int xSpacing = 320;
        const int ySpacing = 120;
        const int padding = 80;

        var rawPositions = new Dictionary<string, DiagramPosition>(StringComparer.Ordinal);
        var hasExplicitCoordinates = model.Nodes.Any(n => n.X.HasValue && n.Y.HasValue);

        if (hasExplicitCoordinates)
        {
            var withCoordinates = model.Nodes.Where(n => n.X.HasValue && n.Y.HasValue).ToArray();
            foreach (var node in withCoordinates)
                rawPositions[node.Id] = new DiagramPosition(node.X!.Value, node.Y!.Value);

            // Keep export deterministic even if some nodes arrive without coordinates.
            var missing = model.Nodes.Where(n => !rawPositions.ContainsKey(n.Id)).ToArray();
            var columns = Math.Clamp((int)Math.Ceiling(Math.Sqrt(Math.Max(1, missing.Length))), 2, 8);
            for (var i = 0; i < missing.Length; i++)
            {
                var row = i / columns;
                var col = i % columns;
                rawPositions[missing[i].Id] = new DiagramPosition(col * xSpacing, row * ySpacing);
            }
        }
        else
        {
            var nodeCount = Math.Max(1, model.Nodes.Count);
            var columns = Math.Clamp((int)Math.Ceiling(Math.Sqrt(nodeCount)), 4, 12);
            var index = 0;
            foreach (var node in model.Nodes)
            {
                var row = index / columns;
                var col = index % columns;
                rawPositions[node.Id] = new DiagramPosition(
                    col * xSpacing,
                    row * ySpacing);
                index++;
            }
        }

        var positions = new Dictionary<string, DiagramPosition>(StringComparer.Ordinal);
        var width = nodeWidth + (padding * 2);
        var height = nodeHeight + (padding * 2);

        if (rawPositions.Count > 0)
        {
            var minX = rawPositions.Values.Min(p => p.X);
            var minY = rawPositions.Values.Min(p => p.Y);
            var maxX = rawPositions.Values.Max(p => p.X);
            var maxY = rawPositions.Values.Max(p => p.Y);

            var contentWidth = Math.Max(1f, (maxX - minX) + nodeWidth);
            var contentHeight = Math.Max(1f, (maxY - minY) + nodeHeight);
            var available = Math.Max(1f, MaxCanvasDimension - (padding * 2f));
            var scale = MathF.Min(1f, MathF.Min(available / contentWidth, available / contentHeight));
            if (!float.IsFinite(scale) || scale <= 0)
                scale = 1f;

            foreach (var (nodeId, point) in rawPositions)
            {
                positions[nodeId] = new DiagramPosition(
                    padding + ((point.X - minX) * scale),
                    padding + ((point.Y - minY) * scale));
            }

            width = (int)MathF.Ceiling((contentWidth * scale) + (padding * 2f));
            height = (int)MathF.Ceiling((contentHeight * scale) + (padding * 2f));
        }

        width = Math.Max(width, nodeWidth + (padding * 2));
        height = Math.Max(height, nodeHeight + (padding * 2));

        return new DiagramExportLayout(
            width,
            height,
            nodeWidth,
            nodeHeight,
            padding,
            positions);
    }

    private static float? TryReadNodeCoordinate(JsonElement node, string propertyName)
    {
        if (TryReadNumber(node, propertyName, out var directValue))
            return directValue;

        if (node.TryGetProperty("position", out var positionElement)
            && positionElement.ValueKind == JsonValueKind.Object
            && TryReadNumber(positionElement, propertyName, out var nestedValue))
        {
            return nestedValue;
        }

        return null;
    }

    private static bool TryReadNumber(JsonElement element, string propertyName, out float value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
            return false;
        if (!property.TryGetDouble(out var raw) || double.IsNaN(raw) || double.IsInfinity(raw))
            return false;
        value = (float)raw;
        return true;
    }
}
