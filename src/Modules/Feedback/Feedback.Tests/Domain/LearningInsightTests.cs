using C4.Modules.Feedback.Domain.Events;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;

namespace C4.Modules.Feedback.Tests.Domain;

public sealed class LearningInsightTests
{
    private static readonly Guid TestProjectId = Guid.NewGuid();

    [Fact]
    public void Aggregate_ValidInput_CreatesInsight()
    {
        var insight = LearningInsight.Aggregate(
            TestProjectId, FeedbackCategory.NodeClassification, InsightType.ClassificationPattern, "Test description", 0.85, 10);

        insight.Should().NotBeNull();
        insight.ProjectId.Should().Be(TestProjectId);
        insight.Category.Should().Be(FeedbackCategory.NodeClassification);
        insight.InsightType.Should().Be(InsightType.ClassificationPattern);
        insight.Description.Should().Be("Test description");
        insight.Confidence.Should().Be(0.85);
        insight.FeedbackCount.Should().Be(10);
    }

    [Fact]
    public void Aggregate_RaisesLearningsAggregatedEvent()
    {
        var insight = LearningInsight.Aggregate(
            TestProjectId, FeedbackCategory.General, InsightType.NamingConvention, "Test", 0.9, 5);

        insight.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LearningsAggregatedEvent>()
            .Which.ProjectId.Should().Be(TestProjectId);
    }

    [Fact]
    public void Aggregate_SetsExpirationThirtyDaysOut()
    {
        var before = DateTime.UtcNow;

        var insight = LearningInsight.Aggregate(
            TestProjectId, FeedbackCategory.General, InsightType.LayoutPreference, "Test", 0.7, 3);

        insight.ExpiresAtUtc.Should().BeAfter(before.AddDays(29));
        insight.ExpiresAtUtc.Should().BeBefore(before.AddDays(31));
    }

    [Fact]
    public void IsExpired_NewInsight_ReturnsFalse()
    {
        var insight = LearningInsight.Aggregate(
            TestProjectId, FeedbackCategory.General, InsightType.ThreatPattern, "Test", 0.8, 4);

        insight.IsExpired.Should().BeFalse();
    }
}
