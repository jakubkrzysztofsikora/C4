using C4.Modules.Discovery.Application.DiscoverResources;

namespace C4.Modules.Discovery.Tests.DiscoverResources;

public sealed class DiscoveryDataPreparerTests
{
    [Fact]
    public void Prepare_McpSourceWithServer_NormalizesProvenanceAndConfidence()
    {
        var preparer = new DiscoveryDataPreparer();

        var result = preparer.Prepare(
            [new RawDiscoveryRecord(null, "Custom/Type", "item", "mcp", null, "GitHubServer")]);

        var prepared = result.Single();
        prepared.SourceProvenance.Should().Be("mcp:githubserver");
        prepared.ConfidenceScore.Should().Be(0.7);
        prepared.StableResourceId.Should().Be("mcp:githubserver:custom/type:item");
    }

    [Fact]
    public void Prepare_ParentRelationship_UsesStableParentId()
    {
        var preparer = new DiscoveryDataPreparer();

        var result = preparer.Prepare(
        [
            new RawDiscoveryRecord("/Parent", "Microsoft.Web/sites", "parent", "azure", null),
            new RawDiscoveryRecord("/Child", "Microsoft.Web/sites/slots", "child", "azure", "/Parent")
        ]);

        var child = result.Single(x => x.RawResourceId == "/Child");
        child.Relationships.Should().ContainSingle();
        child.Relationships.Single().RelationshipType.Should().Be("parent");
        child.Relationships.Single().RelatedStableResourceId.Should().Be("/parent");
    }
}
