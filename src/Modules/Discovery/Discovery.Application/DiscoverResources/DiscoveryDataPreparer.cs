using System.Text;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoveryDataPreparer : IDiscoveryDataPreparer
{
    public IReadOnlyCollection<PreparedDiscoveryRecord> Prepare(IReadOnlyCollection<RawDiscoveryRecord> rawRecords)
    {
        var stableIdMap = rawRecords.ToDictionary(r => r, CreateStableResourceId);

        return rawRecords
            .Select(raw =>
            {
                var relationships = new List<NormalizedRelationship>();
                if (!string.IsNullOrWhiteSpace(raw.ParentResourceId))
                {
                    var normalizedParentId = NormalizeRawResourceId(raw.ParentResourceId);
                    var parentStableId = stableIdMap
                        .Where(pair => string.Equals(NormalizeRawResourceId(pair.Key.ResourceId), normalizedParentId, StringComparison.Ordinal))
                        .Select(pair => pair.Value)
                        .FirstOrDefault() ?? CreateStableResourceId(
                            raw with
                            {
                                ResourceId = raw.ParentResourceId,
                                Name = raw.ParentResourceId!,
                                ParentResourceId = null
                            });

                    relationships.Add(new NormalizedRelationship("parent", parentStableId));
                }

                return new PreparedDiscoveryRecord(
                    stableIdMap[raw],
                    raw.ResourceId,
                    raw.ParentResourceId,
                    raw.ResourceType,
                    raw.Name,
                    NormalizeSource(raw.SourceProvenance, raw.SourceServer),
                    NormalizeConfidence(raw.SourceProvenance, raw.ConfidenceScore),
                    relationships);
            })
            .ToArray();
    }

    private static string CreateStableResourceId(RawDiscoveryRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.ResourceId))
            return NormalizeRawResourceId(record.ResourceId);

        var source = NormalizeSource(record.SourceProvenance, record.SourceServer);
        var normalizedType = record.ResourceType.Trim().ToLowerInvariant();
        var normalizedName = record.Name.Trim().ToLowerInvariant();
        return $"{source}:{normalizedType}:{normalizedName}";
    }

    private static string NormalizeRawResourceId(string? resourceId)
    {
        var trimmed = resourceId?.Trim().ToLowerInvariant() ?? string.Empty;
        if (trimmed.Length == 0)
            return string.Empty;

        var normalized = new StringBuilder(trimmed.Length);
        var previousSlash = false;

        foreach (var ch in trimmed)
        {
            if (ch == '/')
            {
                if (!previousSlash)
                    normalized.Append(ch);

                previousSlash = true;
                continue;
            }

            previousSlash = false;
            normalized.Append(ch);
        }

        return normalized.ToString();
    }

    private static string NormalizeSource(string source, string? server)
    {
        var normalized = source.Trim().ToLowerInvariant();
        if (normalized == "mcp")
            return string.IsNullOrWhiteSpace(server) ? "mcp:unknown" : $"mcp:{server.Trim().ToLowerInvariant()}";

        return normalized;
    }

    private static double NormalizeConfidence(string source, double? confidence)
    {
        if (confidence is >= 0 and <= 1)
            return Math.Round(confidence.Value, 2);

        var normalizedSource = NormalizeSource(source, server: null);
        if (normalizedSource == "azure")
            return 0.95;

        if (normalizedSource == "repo")
            return 0.8;

        if (normalizedSource == "mcp" || normalizedSource.StartsWith("mcp:"))
            return 0.7;

        return 0.6;
    }
}
