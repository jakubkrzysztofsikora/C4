using C4.Modules.Visualization.Api.Persistence;

namespace C4.Modules.Visualization.Tests.Persistence;

[Trait("Category", "Unit")]
public sealed class DiagramExportParserTests
{
    [Fact]
    public void Parse_ValidJsonWithNodes_ReturnsNodes()
    {
        const string json = """
            {
              "nodes": [
                { "id": "n1", "name": "API", "level": "Container" },
                { "id": "n2", "name": "Database", "level": "Container" }
              ],
              "edges": []
            }
            """;

        var model = DiagramExportParser.Parse(json);

        model.Nodes.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_NodeWithSecuritySeverity_IncludesSecuritySeverity()
    {
        const string json = """
            {
              "nodes": [
                { "id": "n1", "name": "API", "level": "Container", "securitySeverity": "high" }
              ],
              "edges": []
            }
            """;

        var model = DiagramExportParser.Parse(json);

        model.Nodes.Should().ContainSingle(n => n.SecuritySeverity == "high");
    }

    [Fact]
    public void Parse_EdgeWithIsDerived_ParsesDerivedFlag()
    {
        const string json = """
            {
              "nodes": [
                { "id": "n1", "name": "API", "level": "Container" },
                { "id": "n2", "name": "DB", "level": "Container" }
              ],
              "edges": [
                { "id": "e1", "sourceNodeId": "n1", "targetNodeId": "n2", "isDerived": true }
              ]
            }
            """;

        var model = DiagramExportParser.Parse(json);

        model.Edges.Should().ContainSingle(e => e.IsDerived);
    }

    [Fact]
    public void Parse_OverlayProperty_IncludesOverlayMode()
    {
        const string json = """
            {
              "nodes": [
                { "id": "n1", "name": "Service", "level": "Container" }
              ],
              "edges": [],
              "overlay": "security"
            }
            """;

        var model = DiagramExportParser.Parse(json);

        model.OverlayMode.Should().Be("security");
    }
}
