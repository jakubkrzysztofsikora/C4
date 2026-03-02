using System.Text.Json;

namespace C4.Modules.Visualization.Api.Persistence;

internal sealed record DiagramExportNode(string Id, string Name, string Level);
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
                var name = node.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                var level = node.TryGetProperty("level", out var levelElement) ? levelElement.GetString() : null;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                    continue;

                nodes.Add(new DiagramExportNode(id, name, string.IsNullOrWhiteSpace(level) ? "Container" : level!));
            }
        }

        var edges = new List<DiagramExportEdge>();
        if (doc.RootElement.TryGetProperty("edges", out var edgesElement) && edgesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var edge in edgesElement.EnumerateArray())
            {
                var id = edge.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var source = edge.TryGetProperty("sourceNodeId", out var sourceElement) ? sourceElement.GetString() : null;
                var target = edge.TryGetProperty("targetNodeId", out var targetElement) ? targetElement.GetString() : null;

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

        var nodeCount = Math.Max(1, model.Nodes.Count);
        var columns = Math.Clamp((int)Math.Ceiling(Math.Sqrt(nodeCount)), 4, 12);
        var rows = Math.Max(1, (int)Math.Ceiling(nodeCount / (double)columns));

        var positions = new Dictionary<string, DiagramPosition>(StringComparer.Ordinal);
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

        var width = (padding * 2) + Math.Max(columns * xSpacing, nodeWidth);
        var height = (padding * 2) + Math.Max(rows * ySpacing, nodeHeight);

        return new DiagramExportLayout(
            width,
            height,
            nodeWidth,
            nodeHeight,
            padding,
            positions);
    }
}
