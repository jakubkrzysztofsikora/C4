using C4.Modules.Feedback.Domain.Corrections;
using C4.Modules.Feedback.Domain.Events;
using C4.Modules.Feedback.Domain.FeedbackEntry;

namespace C4.Modules.Feedback.Tests.Domain;

public sealed class FeedbackEntryTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestProjectId = Guid.NewGuid();
    private static readonly Guid TestTargetId = Guid.NewGuid();
    private static readonly FeedbackTarget TestTarget = new(FeedbackTargetType.GraphNode, TestTargetId);
    private static readonly FeedbackRating ValidRating = FeedbackRating.Create(4).Value;

    [Fact]
    public void Submit_ValidInput_CreatesFeedbackEntry()
    {
        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.NodeClassification, ValidRating, "test comment", null, null, null);

        entry.Should().NotBeNull();
        entry.Id.Should().NotBeNull();
        entry.UserId.Should().Be(TestUserId);
        entry.Target.Should().Be(TestTarget);
        entry.Category.Should().Be(FeedbackCategory.NodeClassification);
        entry.Rating.Should().Be(ValidRating);
        entry.Comment.Should().Be("test comment");
    }

    [Fact]
    public void Submit_SetsSubmittedAtUtc()
    {
        var before = DateTime.UtcNow;

        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.General, ValidRating, null, null, null, null);

        var after = DateTime.UtcNow;
        entry.SubmittedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Submit_RaisesFeedbackSubmittedEvent()
    {
        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.DiagramLayout, ValidRating, null, null, null, null);

        entry.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<FeedbackSubmittedEvent>()
            .Which.FeedbackEntryId.Should().Be(entry.Id);
    }

    [Fact]
    public void Submit_FeedbackSubmittedEvent_ContainsUserId()
    {
        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.General, ValidRating, null, null, null, null);

        entry.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<FeedbackSubmittedEvent>()
            .Which.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public void Submit_WithNodeCorrection_IncludesCorrection()
    {
        var correction = new NodeCorrection("OldName", "NewName", "Container", "Component", null, null, null, null);

        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.NodeClassification, ValidRating, null, correction, null, null);

        entry.NodeCorrection.Should().Be(correction);
    }

    [Fact]
    public void Submit_WithEdgeCorrection_IncludesCorrection()
    {
        var correction = new EdgeCorrection("http", "https", true);

        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.EdgeRelationship, ValidRating, null, null, correction, null);

        entry.EdgeCorrection.Should().Be(correction);
    }

    [Fact]
    public void Submit_WithClassificationCorrection_IncludesCorrection()
    {
        var correction = new ClassificationCorrection(
            "Microsoft.Web/sites", "Web App", "App Service", null, "app", "Container", "Container", true, true);

        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.ResourceClassification, ValidRating, null, null, null, correction);

        entry.ClassificationCorrection.Should().Be(correction);
    }

    [Fact]
    public void Submit_WithoutCorrections_CorrectionsAreNull()
    {
        var entry = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.General, ValidRating, "general comment", null, null, null);

        entry.NodeCorrection.Should().BeNull();
        entry.EdgeCorrection.Should().BeNull();
        entry.ClassificationCorrection.Should().BeNull();
    }

    [Fact]
    public void Submit_GeneratesUniqueIds()
    {
        var entry1 = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.General, ValidRating, null, null, null, null);

        var entry2 = FeedbackEntry.Submit(
            TestUserId, TestProjectId, TestTarget, FeedbackCategory.General, ValidRating, null, null, null, null);

        entry1.Id.Should().NotBe(entry2.Id);
    }
}
