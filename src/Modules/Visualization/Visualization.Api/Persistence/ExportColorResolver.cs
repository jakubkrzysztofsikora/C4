namespace C4.Modules.Visualization.Api.Persistence;

internal static class ExportColorResolver
{
    internal static string ResolveNodeStroke(DiagramExportNode node, string? overlayMode)
        => overlayMode switch
        {
            "threat" => RiskColor(node.RiskLevel),
            "cost" => CostColor(node.HourlyCostUsd),
            "security" => SecurityColor(node.SecuritySeverity),
            _ => "#3c6fb3"
        };

    internal static string ResolveEdgeStroke(DiagramExportEdge edge, string? overlayMode)
        => TrafficColor(edge.TrafficState);

    internal static string TrafficColor(string? trafficState)
        => trafficState switch
        {
            "green" => "#2e8f5e",
            "yellow" => "#9d7c35",
            "red" => "#9e3a3a",
            _ => "#4a7dc2"
        };

    internal static string RiskColor(string? risk)
        => risk switch
        {
            "critical" => "#7f1d1d",
            "high" => "#b91c1c",
            "medium" => "#b45309",
            "low" => "#166534",
            _ => "#334155"
        };

    internal static string CostColor(double? cost)
    {
        if (cost is null) return "#64748b";
        if (cost >= 1.0) return "#b91c1c";
        if (cost >= 0.35) return "#b45309";
        return "#2e8f5e";
    }

    internal static string SecurityColor(string? severity)
        => severity switch
        {
            "critical" => "#7f1d1d",
            "high" => "#b91c1c",
            "medium" => "#b45309",
            "low" => "#2e8f5e",
            _ => "#6b7280"
        };

    internal static (string Label, string Color)[] BuildLegend(string? overlayMode)
        => overlayMode switch
        {
            "threat" => [("Critical", "#7f1d1d"), ("High", "#b91c1c"), ("Medium", "#b45309"), ("Low", "#166534")],
            "cost" => [("High (>$1/hr)", "#b91c1c"), ("Medium ($0.35-$1)", "#b45309"), ("Low (<$0.35)", "#2e8f5e")],
            "security" => [("Critical", "#7f1d1d"), ("High", "#b91c1c"), ("Medium", "#b45309"), ("Low", "#2e8f5e"), ("None/Unknown", "#6b7280")],
            _ => []
        };
}
