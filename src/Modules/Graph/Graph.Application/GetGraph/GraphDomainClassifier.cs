namespace C4.Modules.Graph.Application.GetGraph;

internal static class GraphDomainClassifier
{
    public static string InferDomain(string? explicitDomain, string name, string resourceGroup)
    {
        if (!string.IsNullOrWhiteSpace(explicitDomain) && !explicitDomain.Equals("General", StringComparison.OrdinalIgnoreCase))
            return explicitDomain!;

        string combined = $"{resourceGroup} {name}".ToLowerInvariant();
        if (combined.Contains("document-service", StringComparison.Ordinal))
            return "DocumentService";
        if (combined.Contains("-ob", StringComparison.Ordinal)
            || combined.Contains("openbank", StringComparison.Ordinal)
            || combined.Contains("banking", StringComparison.Ordinal))
            return "OpenBanking";
        if (combined.Contains("grafana", StringComparison.Ordinal)
            || combined.Contains("monitoring", StringComparison.Ordinal)
            || combined.Contains("mcp", StringComparison.Ordinal))
            return "Platform";
        if (combined.Contains("circit-", StringComparison.Ordinal)
            || combined.Contains("coreapp", StringComparison.Ordinal)
            || combined.Contains("app-circit", StringComparison.Ordinal))
            return "CoreApp";
        return "General";
    }
}
