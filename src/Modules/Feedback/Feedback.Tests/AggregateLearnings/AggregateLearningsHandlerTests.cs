using C4.Modules.Feedback.Application.AggregateLearnings;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;
using C4.Modules.Feedback.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace C4.Modules.Feedback.Tests.AggregateLearnings;

public sealed class AggregateLearningsHandlerTests
{
    private readonly InMemoryFeedbackEntryRepository _feedbackRepository = new();
    private readonly FakeLearningAggregator _aggregator = new();
    private readonly InMemoryLearningInsightRepository _insightRepository = new();
    private readonly InMemoryFeedbackUnitOfWork _unitOfWork = new();
    private readonly NullMediator _mediator = new();
    private readonly Guid _projectId = Guid.NewGuid();

    private AggregateLearningsHandler CreateHandler() =>
        new(_feedbackRepository, _aggregator, _insightRepository, _unitOfWork, _mediator, NullLogger<AggregateLearningsHandler>.Instance);

    [Fact]
    public async Task Handle_WithFeedback_CreatesInsights()
    {
        var target = new FeedbackTarget(FeedbackTargetType.GraphNode, Guid.NewGuid());
        await _feedbackRepository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.NodeClassification, FeedbackRating.Create(3).Value, null, null, null, null),
            CancellationToken.None);
        var insight = LearningInsight.Aggregate(_projectId, FeedbackCategory.NodeClassification, InsightType.ClassificationPattern, "Users prefer Container level", 0.85, 5);
        _aggregator.SetInsights(insight);

        var result = await CreateHandler().Handle(new AggregateLearningsCommand(_projectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InsightsGenerated.Should().Be(1);
        _insightRepository.All.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoFeedback_ReturnsZeroInsights()
    {
        var result = await CreateHandler().Handle(new AggregateLearningsCommand(_projectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InsightsGenerated.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CategoryFilter_AggregatesOnlyMatchingFeedback()
    {
        var target = new FeedbackTarget(FeedbackTargetType.GraphNode, Guid.NewGuid());
        await _feedbackRepository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.NodeClassification, FeedbackRating.Create(4).Value, null, null, null, null),
            CancellationToken.None);
        await _feedbackRepository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.EdgeRelationship, FeedbackRating.Create(2).Value, null, null, null, null),
            CancellationToken.None);
        var insight = LearningInsight.Aggregate(_projectId, FeedbackCategory.NodeClassification, InsightType.NamingConvention, "Consistent naming", 0.9, 3);
        _aggregator.SetInsights(insight);

        var result = await CreateHandler().Handle(
            new AggregateLearningsCommand(_projectId, FeedbackCategory.NodeClassification),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InsightsGenerated.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PersistsAndSavesChanges()
    {
        var target = new FeedbackTarget(FeedbackTargetType.Diagram, Guid.NewGuid());
        await _feedbackRepository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.DiagramLayout, FeedbackRating.Create(3).Value, null, null, null, null),
            CancellationToken.None);
        var insight1 = LearningInsight.Aggregate(_projectId, FeedbackCategory.DiagramLayout, InsightType.LayoutPreference, "Left-to-right preferred", 0.8, 10);
        var insight2 = LearningInsight.Aggregate(_projectId, FeedbackCategory.DiagramLayout, InsightType.LayoutPreference, "Group by domain", 0.7, 8);
        _aggregator.SetInsights(insight1, insight2);

        await CreateHandler().Handle(new AggregateLearningsCommand(_projectId, null), CancellationToken.None);

        _insightRepository.All.Should().HaveCount(2);
        _unitOfWork.SaveChangesCount.Should().Be(1);
    }
}
