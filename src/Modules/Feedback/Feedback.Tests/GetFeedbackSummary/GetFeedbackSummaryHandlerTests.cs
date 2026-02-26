using C4.Modules.Feedback.Application.GetFeedbackSummary;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Tests.Fakes;

namespace C4.Modules.Feedback.Tests.GetFeedbackSummary;

public sealed class GetFeedbackSummaryHandlerTests
{
    private readonly InMemoryFeedbackEntryRepository _repository = new();
    private readonly Guid _projectId = Guid.NewGuid();

    private GetFeedbackSummaryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_NoFeedback_ReturnsZeroCounts()
    {
        var result = await CreateHandler().Handle(new GetFeedbackSummaryQuery(_projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.AverageRating.Should().Be(0);
        result.Value.CategoryBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasFeedback_ReturnsAggregatedSummary()
    {
        var target = new FeedbackTarget(FeedbackTargetType.GraphNode, Guid.NewGuid());
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.NodeClassification, FeedbackRating.Create(4).Value, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.NodeClassification, FeedbackRating.Create(2).Value, null, null, null, null),
            CancellationToken.None);

        var result = await CreateHandler().Handle(new GetFeedbackSummaryQuery(_projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.AverageRating.Should().Be(3);
    }

    [Fact]
    public async Task Handle_MultipleCategories_ReturnsBreakdown()
    {
        var target = new FeedbackTarget(FeedbackTargetType.GraphNode, Guid.NewGuid());
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.NodeClassification, FeedbackRating.Create(5).Value, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.EdgeRelationship, FeedbackRating.Create(3).Value, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.ThreatAssessment, FeedbackRating.Create(1).Value, null, null, null, null),
            CancellationToken.None);

        var result = await CreateHandler().Handle(new GetFeedbackSummaryQuery(_projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryBreakdown.Should().HaveCount(3);
        result.Value.CategoryBreakdown.Should().Contain(b => b.Category == "NodeClassification" && b.AverageRating == 5);
        result.Value.CategoryBreakdown.Should().Contain(b => b.Category == "EdgeRelationship" && b.AverageRating == 3);
        result.Value.CategoryBreakdown.Should().Contain(b => b.Category == "ThreatAssessment" && b.AverageRating == 1);
    }
}
