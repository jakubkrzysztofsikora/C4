using C4.Modules.Visualization.Api.Persistence;
using FluentAssertions.Execution;

namespace C4.Modules.Visualization.Tests.Persistence;

[Trait("Category", "Unit")]
public sealed class ExportColorResolverTests
{
    [Fact]
    public void SecurityColor_Critical_ReturnsDarkRed()
    {
        var color = ExportColorResolver.SecurityColor("critical");

        color.Should().Be("#7f1d1d");
    }

    [Fact]
    public void SecurityColor_High_ReturnsRed()
    {
        var color = ExportColorResolver.SecurityColor("high");

        color.Should().Be("#b91c1c");
    }

    [Fact]
    public void SecurityColor_Medium_ReturnsAmber()
    {
        var color = ExportColorResolver.SecurityColor("medium");

        color.Should().Be("#b45309");
    }

    [Fact]
    public void SecurityColor_Low_ReturnsGreen()
    {
        var color = ExportColorResolver.SecurityColor("low");

        color.Should().Be("#2e8f5e");
    }

    [Fact]
    public void SecurityColor_Null_ReturnsGray()
    {
        var color = ExportColorResolver.SecurityColor(null);

        color.Should().Be("#6b7280");
    }

    [Fact]
    public void SecurityColor_Unknown_ReturnsGray()
    {
        var color = ExportColorResolver.SecurityColor("unknown");

        color.Should().Be("#6b7280");
    }

    [Fact]
    public void RiskColor_Critical_ReturnsDarkRed()
    {
        var color = ExportColorResolver.RiskColor("critical");

        color.Should().Be("#7f1d1d");
    }

    [Fact]
    public void RiskColor_High_ReturnsRed()
    {
        var color = ExportColorResolver.RiskColor("high");

        color.Should().Be("#b91c1c");
    }

    [Fact]
    public void RiskColor_Null_ReturnsSlate()
    {
        var color = ExportColorResolver.RiskColor(null);

        color.Should().Be("#334155");
    }

    [Fact]
    public void CostColor_Null_ReturnsSlateGray()
    {
        var color = ExportColorResolver.CostColor(null);

        color.Should().Be("#64748b");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(100.0)]
    public void CostColor_HighCost_ReturnsRed(double cost)
    {
        var color = ExportColorResolver.CostColor(cost);

        color.Should().Be("#b91c1c");
    }

    [Theory]
    [InlineData(0.35)]
    [InlineData(0.5)]
    [InlineData(0.99)]
    public void CostColor_MediumCost_ReturnsAmber(double cost)
    {
        var color = ExportColorResolver.CostColor(cost);

        color.Should().Be("#b45309");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.2)]
    [InlineData(0.34)]
    public void CostColor_LowCost_ReturnsGreen(double cost)
    {
        var color = ExportColorResolver.CostColor(cost);

        color.Should().Be("#2e8f5e");
    }

    [Fact]
    public void TrafficColor_Green_ReturnsGreen()
    {
        var color = ExportColorResolver.TrafficColor("green");

        color.Should().Be("#2e8f5e");
    }

    [Fact]
    public void TrafficColor_Red_ReturnsRed()
    {
        var color = ExportColorResolver.TrafficColor("red");

        color.Should().Be("#9e3a3a");
    }

    [Fact]
    public void TrafficColor_Null_ReturnsBlue()
    {
        var color = ExportColorResolver.TrafficColor(null);

        color.Should().Be("#4a7dc2");
    }

    [Fact]
    public void ResolveNodeStroke_ThreatOverlay_UsesRiskColor()
    {
        var node = new DiagramExportNode("n1", "Service", "Container", RiskLevel: "high");

        var color = ExportColorResolver.ResolveNodeStroke(node, "threat");

        color.Should().Be(ExportColorResolver.RiskColor("high"));
    }

    [Fact]
    public void ResolveNodeStroke_CostOverlay_UsesCostColor()
    {
        var node = new DiagramExportNode("n1", "Service", "Container", HourlyCostUsd: 0.5);

        var color = ExportColorResolver.ResolveNodeStroke(node, "cost");

        color.Should().Be(ExportColorResolver.CostColor(0.5));
    }

    [Fact]
    public void ResolveNodeStroke_SecurityOverlay_UsesSecurityColor()
    {
        var node = new DiagramExportNode("n1", "Service", "Container", SecuritySeverity: "critical");

        var color = ExportColorResolver.ResolveNodeStroke(node, "security");

        color.Should().Be(ExportColorResolver.SecurityColor("critical"));
    }

    [Fact]
    public void ResolveNodeStroke_NoOverlay_ReturnsDefaultBlue()
    {
        var node = new DiagramExportNode("n1", "Service", "Container");

        var color = ExportColorResolver.ResolveNodeStroke(node, null);

        color.Should().Be("#3c6fb3");
    }

    [Fact]
    public void BuildLegend_ThreatOverlay_ReturnsFourEntries()
    {
        var legend = ExportColorResolver.BuildLegend("threat");

        legend.Should().HaveCount(4);
    }

    [Fact]
    public void BuildLegend_SecurityOverlay_ReturnsFiveEntries()
    {
        var legend = ExportColorResolver.BuildLegend("security");

        using var scope = new AssertionScope();
        legend.Should().HaveCount(5);
        legend.Should().Contain(e => e.Label == "None/Unknown" && e.Color == "#6b7280");
    }

    [Fact]
    public void BuildLegend_CostOverlay_ReturnsThreeEntries()
    {
        var legend = ExportColorResolver.BuildLegend("cost");

        legend.Should().HaveCount(3);
    }

    [Fact]
    public void BuildLegend_NoOverlay_ReturnsEmpty()
    {
        var legend = ExportColorResolver.BuildLegend(null);

        legend.Should().BeEmpty();
    }
}
