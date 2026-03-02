using C4.Modules.Discovery.Domain.Resources;
using C4.Modules.Graph.Domain.GraphNode;

namespace C4.Modules.Graph.Application.GetGraph;

internal static class GraphClassificationResolver
{
    public static ResolvedNodeClassification Resolve(GraphNode node, string resourceGroup)
    {
        string armType = ExtractArmType(node.ExternalResourceId);
        AzureResourceClassification classification = AzureResourceTypeCatalog.Classify(armType);

        string serviceType = classification.ServiceType;
        string c4Level = classification.C4Level;
        string source = classification.ClassificationSource;
        double confidence = classification.Confidence;
        bool isInfrastructure = classification.IsInfrastructure;

        if (classification.ClassificationSource.Equals("fallback", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(node.Properties.Technology)
            && !node.Properties.Technology.Equals("external", StringComparison.OrdinalIgnoreCase))
        {
            serviceType = node.Properties.Technology;
            c4Level = node.Level.ToString();
            source = string.IsNullOrWhiteSpace(node.Properties.ClassificationSource)
                ? "legacy"
                : node.Properties.ClassificationSource;
            confidence = node.Properties.ClassificationConfidence > 0 ? node.Properties.ClassificationConfidence : 0.75;
            isInfrastructure = node.Properties.IsInfrastructure;
        }

        return new ResolvedNodeClassification(
            armType,
            c4Level,
            serviceType,
            isInfrastructure,
            source,
            confidence);
    }

    internal static string ExtractArmType(string resourceId)
    {
        string lower = resourceId.ToLowerInvariant();
        int providersIndex = lower.IndexOf("/providers/", StringComparison.Ordinal);
        if (providersIndex < 0)
        {
            if (lower.Contains("/resourcegroups/", StringComparison.Ordinal))
                return "microsoft.resources/resourcegroups";
            if (lower.StartsWith("/subscriptions/", StringComparison.Ordinal))
                return "microsoft.resources/subscriptions";
            return "unknown/unknown";
        }

        string[] segments = lower[(providersIndex + "/providers/".Length)..]
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return "unknown/unknown";

        string ns = segments[0];
        List<string> types = [];
        for (int i = 1; i < segments.Length; i += 2)
        {
            types.Add(segments[i]);
        }

        return $"{ns}/{string.Join("/", types)}";
    }
}

internal sealed record ResolvedNodeClassification(
    string ArmType,
    string C4Level,
    string ServiceType,
    bool IsInfrastructure,
    string ClassificationSource,
    double ClassificationConfidence);
