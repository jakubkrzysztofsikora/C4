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

        var positions = new Dictionary<string, DiagramPosition>(StringComparer.Ordinal);
        var hasExplicitCoordinates = model.Nodes.Any(n => n.X.HasValue && n.Y.HasValue);

        if (hasExplicitCoordinates)
        {
            foreach (var node in model.Nodes)
            {
                if (node.X.HasValue && node.Y.HasValue)
                {
                    positions[node.Id] = new DiagramPosition(node.X.Value + padding, node.Y.Value + padding);
                }
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
                positions[node.Id] = new DiagramPosition(
                    padding + (col * xSpacing),
                    padding + (row * ySpacing));
                index++;
            }
        }

        var maxX = positions.Values.DefaultIfEmpty(new DiagramPosition(0, 0)).Max(p => p.X);
        var maxY = positions.Values.DefaultIfEmpty(new DiagramPosition(0, 0)).Max(p => p.Y);
        var width = (int)Math.Ceiling(maxX + nodeWidth + padding);
        var height = (int)Math.Ceiling(maxY + nodeHeight + padding);
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
        if (node.TryGetProperty(propertyName, out var directCoordinate) && directCoordinate.TryGetSingle(out var directValue))
            return directValue;

        if (node.TryGetProperty("position", out var positionElement)
            && positionElement.ValueKind == JsonValueKind.Object
            && positionElement.TryGetProperty(propertyName, out var nestedCoordinate)
            && nestedCoordinate.TryGetSingle(out var nestedValue))
        {
            return nestedValue;
        }

        return null;
    }
}
