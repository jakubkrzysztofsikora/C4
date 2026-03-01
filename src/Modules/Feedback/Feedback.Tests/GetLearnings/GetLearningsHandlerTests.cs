using C4.Modules.Feedback.Application.GetLearnings;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;
using C4.Modules.Feedback.Tests.Fakes;

namespace C4.Modules.Feedback.Tests.GetLearnings;

public sealed class GetLearningsHandlerTests
{
    private readonly InMemoryLearningInsightRepository _repository = new();
    private readonly Guid _projectId = Guid.NewGuid();

    private GetLearningsHandler CreateHandler() => new(_repository, new AlwaysAuthorizingService());

    [Fact]
    public async Task Handle_HasActiveInsights_ReturnsInsights()
    {
        var insight = LearningInsight.Aggregate(_projectId, FeedbackCategory.NodeClassification, InsightType.ClassificationPattern, "Container nodes preferred", 0.9, 5);
        await _repository.AddAsync(insight, CancellationToken.None);

        var result = await CreateHandler().Handle(new GetLearningsQuery(_projectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Insights.Should().ContainSingle();
        result.Value.Insights.First().Description.Should().Be("Container nodes preferred");
    }

    [Fact]
    public async Task Handle_NoInsights_ReturnsEmptyList()
    {
        var result = await CreateHandler().Handle(new GetLearningsQuery(_projectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Insights.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CategoryFilter_ReturnsFilteredInsights()
    {
        await _repository.AddAsync(
            LearningInsight.Aggregate(_projectId, FeedbackCategory.NodeClassification, InsightType.ClassificationPattern, "Insight A", 0.8, 3),
            CancellationToken.None);
        await _repository.AddAsync(
            LearningInsight.Aggregate(_projectId, FeedbackCategory.EdgeRelationship, InsightType.RelationshipRule, "Insight B", 0.7, 2),
            CancellationToken.None);

        var result = await CreateHandler().Handle(
            new GetLearningsQuery(_projectId, FeedbackCategory.NodeClassification),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Insights.Should().ContainSingle();
        result.Value.Insights.First().Description.Should().Be("Insight A");
    }
}
