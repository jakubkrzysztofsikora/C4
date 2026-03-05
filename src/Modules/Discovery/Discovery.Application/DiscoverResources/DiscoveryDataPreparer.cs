using System.Text;
using System.Security.Cryptography;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoveryDataPreparer : IDiscoveryDataPreparer
{
    private const int ResourceIdMaxLength = 500;
    private const int ResourceTypeMaxLength = 200;
    private const int ResourceNameMaxLength = 250;

    public IReadOnlyCollection<PreparedDiscoveryRecord> Prepare(IReadOnlyCollection<RawDiscoveryRecord> rawRecords)
    {
        var stableIdMap = rawRecords.ToDictionary(r => r, CreateStableResourceId);

        var stableIdByNormalizedId = rawRecords
            .Where(r => r.ResourceId is not null)
            .GroupBy(r => NormalizeRawResourceId(r.ResourceId!))
            .ToDictionary(g => g.Key, g => stableIdMap[g.First()]);

        return rawRecords
            .Select(raw =>
            {
                var relationships = new List<NormalizedRelationship>();
                if (!string.IsNullOrWhiteSpace(raw.ParentResourceId))
                {
                    var normalizedParentId = NormalizeRawResourceId(raw.ParentResourceId);
                    var parentStableId = stableIdByNormalizedId.GetValueOrDefault(normalizedParentId)
                        ?? CreateStableResourceId(
                            raw with
                            {
                                ResourceId = raw.ParentResourceId,
                                Name = raw.ParentResourceId!,
                                ParentResourceId = null
                            });

                    relationships.Add(new NormalizedRelationship("parent", parentStableId));
                }

                if (raw.RawPropertyReferences is { Count: > 0 })
                {
                    foreach (string refId in raw.RawPropertyReferences)
                    {
                        string normalizedRefId = NormalizeRawResourceId(refId);
                        if (!string.IsNullOrWhiteSpace(normalizedRefId))
                            relationships.Add(new NormalizedRelationship("property-ref", normalizedRefId));
                    }
                }

                return new PreparedDiscoveryRecord(
                    stableIdMap[raw],
                    raw.ResourceId,
                    raw.ParentResourceId,
                    Truncate(raw.ResourceType, ResourceTypeMaxLength),
                    Truncate(raw.Name, ResourceNameMaxLength),
                    NormalizeSource(raw.SourceProvenance, raw.SourceServer),
                    NormalizeConfidence(raw.SourceProvenance, raw.ConfidenceScore),
                    relationships,
                    raw.ResourceGroup,
                    raw.Tags);
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

        return NormalizeIdentifier(normalized.ToString(), ResourceIdMaxLength);
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

    private static string Truncate(string? value, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length <= maxLength)
            return trimmed;

        return trimmed[..maxLength];
    }

    private static string NormalizeIdentifier(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
        var separator = "~";
        var prefixLength = Math.Max(1, maxLength - hash.Length - separator.Length);
        return $"{value[..prefixLength]}{separator}{hash}";
    }
}
