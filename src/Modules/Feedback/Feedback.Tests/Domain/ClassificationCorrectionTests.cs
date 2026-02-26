using C4.Modules.Feedback.Domain.Corrections;

namespace C4.Modules.Feedback.Tests.Domain;

public sealed class ClassificationCorrectionTests
{
    [Fact]
    public void Create_WithCorrectedServiceType_CapturesChange()
    {
        var correction = new ClassificationCorrection(
            "Microsoft.Web/sites", null, null, "external", "app", null, null, null, null);

        correction.ArmResourceType.Should().Be("Microsoft.Web/sites");
        correction.OriginalServiceType.Should().Be("external");
        correction.CorrectedServiceType.Should().Be("app");
    }

    [Fact]
    public void Create_WithInclusionChange_CapturesChange()
    {
        var correction = new ClassificationCorrection(
            "Microsoft.Network/networkInterfaces", null, null, null, null, null, null, false, true);

        correction.OriginalIncludeInDiagram.Should().BeFalse();
        correction.CorrectedIncludeInDiagram.Should().BeTrue();
    }

    [Fact]
    public void Create_WithCorrectedFriendlyName_CapturesChange()
    {
        var correction = new ClassificationCorrection(
            "Microsoft.DBforPostgreSQL/flexibleServers", "Postgres", "PostgreSQL", null, null, null, null, null, null);

        correction.OriginalFriendlyName.Should().Be("Postgres");
        correction.CorrectedFriendlyName.Should().Be("PostgreSQL");
    }

    [Fact]
    public void Create_WithCorrectedC4Level_CapturesChange()
    {
        var correction = new ClassificationCorrection(
            "Microsoft.Web/sites/functions", null, null, null, null, "Container", "Component", null, null);

        correction.OriginalC4Level.Should().Be("Container");
        correction.CorrectedC4Level.Should().Be("Component");
    }

    [Fact]
    public void ValueEquality_SameValues_AreEqual()
    {
        var correction1 = new ClassificationCorrection(
            "Microsoft.Web/sites", "Web App", "App Service", "external", "app", "Container", "Container", true, true);
        var correction2 = new ClassificationCorrection(
            "Microsoft.Web/sites", "Web App", "App Service", "external", "app", "Container", "Container", true, true);

        correction1.Should().Be(correction2);
    }
}
