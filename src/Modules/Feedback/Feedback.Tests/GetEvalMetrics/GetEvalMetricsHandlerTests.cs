using C4.Modules.Feedback.Application.GetEvalMetrics;
using C4.Modules.Feedback.Domain.Corrections;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Tests.Fakes;

namespace C4.Modules.Feedback.Tests.GetEvalMetrics;

public sealed class GetEvalMetricsHandlerTests
{
    private readonly InMemoryFeedbackEntryRepository _repository = new();
    private readonly Guid _projectId = Guid.NewGuid();

    private GetEvalMetricsHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_NoData_ReturnsEmptyMetrics()
    {
        var result = await CreateHandler().Handle(new GetEvalMetricsQuery(_projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFeedbackCount.Should().Be(0);
        result.Value.OverallAverageRating.Should().Be(0);
        result.Value.CorrectionRate.Should().Be(0);
        result.Value.WeeklyTrends.Should().BeEmpty();
        result.Value.PerPluginBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithFeedback_ReturnsCorrectTotalAndAverage()
    {
        var target = new FeedbackTarget(FeedbackTargetType.Diagram, Guid.NewGuid());
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.DiagramLayout, FeedbackRating.Create(4).Value, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.DiagramLayout, FeedbackRating.Create(2).Value, null, null, null, null),
            CancellationToken.None);

        var result = await CreateHandler().Handle(new GetEvalMetricsQuery(_projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFeedbackCount.Should().Be(2);
        result.Value.OverallAverageRating.Should().Be(3.0);
    }

    [Fact]
    public async Task Handle_WithCorrections_CalculatesCorrectionRate()
    {
        var target = new FeedbackTarget(FeedbackTargetType.GraphNode, Guid.NewGuid());
        var correction = new NodeCorrection("Old", "New", null, null, null, null, null, null);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.NodeClassification, FeedbackRating.Create(3).Value, null, correction, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.NodeClassification, FeedbackRating.Create(4).Value, null, null, null, null),
            CancellationToken.None);

        var result = await CreateHandler().Handle(new GetEvalMetricsQuery(_projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CorrectionRate.Should().Be(0.5);
    }

    [Fact]
    public async Task Handle_PerPluginBreakdown_SplitsByCategory()
    {
        var target = new FeedbackTarget(FeedbackTargetType.Diagram, Guid.NewGuid());
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.ArchitectureAnalysis, FeedbackRating.Create(5).Value, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.ArchitectureAnalysis, FeedbackRating.Create(3).Value, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), target, FeedbackCategory.ThreatAssessment, FeedbackRating.Create(4).Value, null, null, null, null),
            CancellationToken.None);

        var result = await CreateHandler().Handle(new GetEvalMetricsQuery(_projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PerPluginBreakdown.Should().HaveCount(2);
        var archMetrics = result.Value.PerPluginBreakdown.First(p => p.Category == "ArchitectureAnalysis");
        archMetrics.FeedbackCount.Should().Be(2);
        archMetrics.AverageRating.Should().Be(4.0);
        var threatMetrics = result.Value.PerPluginBreakdown.First(p => p.Category == "ThreatAssessment");
        threatMetrics.FeedbackCount.Should().Be(1);
        threatMetrics.AverageRating.Should().Be(4.0);
    }
}
