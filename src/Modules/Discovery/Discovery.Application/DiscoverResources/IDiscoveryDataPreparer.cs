namespace C4.Modules.Discovery.Application.DiscoverResources;

public interface IDiscoveryDataPreparer
{
    IReadOnlyCollection<PreparedDiscoveryRecord> Prepare(IReadOnlyCollection<RawDiscoveryRecord> rawRecords);
}

public sealed record RawDiscoveryRecord(
    string? ResourceId,
    string ResourceType,
    string Name,
    string SourceProvenance,
    string? ParentResourceId,
    string? SourceServer = null,
    double? ConfidenceScore = null);

public sealed record NormalizedRelationship(string RelationshipType, string RelatedStableResourceId);

public sealed record PreparedDiscoveryRecord(
    string StableResourceId,
    string? RawResourceId,
    string? RawParentResourceId,
    string ResourceType,
    string Name,
    string SourceProvenance,
    double ConfidenceScore,
    IReadOnlyCollection<NormalizedRelationship> Relationships);
