using C4.Modules.Feedback.Domain.Corrections;

namespace C4.Modules.Feedback.Tests.Domain;

public sealed class NodeCorrectionTests
{
    [Fact]
    public void Create_WithCorrectedLevel_CapturesChange()
    {
        var correction = new NodeCorrection(null, null, "Container", "Component", null, null, null, null);

        correction.OriginalLevel.Should().Be("Container");
        correction.CorrectedLevel.Should().Be("Component");
    }

    [Fact]
    public void Create_WithCorrectedName_CapturesChange()
    {
        var correction = new NodeCorrection("OldService", "NewService", null, null, null, null, null, null);

        correction.OriginalName.Should().Be("OldService");
        correction.CorrectedName.Should().Be("NewService");
    }

    [Fact]
    public void Create_WithCorrectedParent_CapturesChange()
    {
        Guid originalParent = Guid.NewGuid();
        Guid correctedParent = Guid.NewGuid();

        var correction = new NodeCorrection(null, null, null, null, null, null, originalParent, correctedParent);

        correction.OriginalParentId.Should().Be(originalParent);
        correction.CorrectedParentId.Should().Be(correctedParent);
    }

    [Fact]
    public void Create_WithCorrectedServiceType_CapturesChange()
    {
        var correction = new NodeCorrection(null, null, null, null, "external", "database", null, null);

        correction.OriginalServiceType.Should().Be("external");
        correction.CorrectedServiceType.Should().Be("database");
    }

    [Fact]
    public void ValueEquality_SameValues_AreEqual()
    {
        var correction1 = new NodeCorrection("A", "B", "Container", "Component", null, null, null, null);
        var correction2 = new NodeCorrection("A", "B", "Container", "Component", null, null, null, null);

        correction1.Should().Be(correction2);
    }
}
