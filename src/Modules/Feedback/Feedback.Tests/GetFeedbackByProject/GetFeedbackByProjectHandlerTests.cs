using C4.Modules.Feedback.Application.GetFeedbackByProject;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Tests.Fakes;

namespace C4.Modules.Feedback.Tests.GetFeedbackByProject;

public sealed class GetFeedbackByProjectHandlerTests
{
    private readonly InMemoryFeedbackEntryRepository _repository = new();
    private readonly Guid _projectId = Guid.NewGuid();

    private GetFeedbackByProjectHandler CreateHandler() => new(_repository, new AlwaysAuthorizingService());

    [Fact]
    public async Task Handle_HasFeedback_ReturnsPaginatedList()
    {
        var rating = FeedbackRating.Create(4).Value;
        var target = new FeedbackTarget(FeedbackTargetType.GraphNode, Guid.NewGuid());
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.NodeClassification, rating, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.EdgeRelationship, rating, null, null, null, null),
            CancellationToken.None);

        var result = await CreateHandler().Handle(new GetFeedbackByProjectQuery(_projectId, 0, 10, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyList()
    {
        var result = await CreateHandler().Handle(new GetFeedbackByProjectQuery(_projectId, 0, 10, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CategoryFilter_ReturnsFilteredResults()
    {
        var rating = FeedbackRating.Create(3).Value;
        var target = new FeedbackTarget(FeedbackTargetType.GraphNode, Guid.NewGuid());
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.NodeClassification, rating, null, null, null, null),
            CancellationToken.None);
        await _repository.AddAsync(
            FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.EdgeRelationship, rating, null, null, null, null),
            CancellationToken.None);

        var result = await CreateHandler().Handle(
            new GetFeedbackByProjectQuery(_projectId, 0, 10, FeedbackCategory.NodeClassification),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().ContainSingle();
        result.Value.Entries.First().Category.Should().Be(FeedbackCategory.NodeClassification);
    }

    [Fact]
    public async Task Handle_Pagination_RespectsSkipAndTake()
    {
        var rating = FeedbackRating.Create(4).Value;
        var target = new FeedbackTarget(FeedbackTargetType.Diagram, Guid.NewGuid());
        for (int i = 0; i < 5; i++)
        {
            await _repository.AddAsync(
                FeedbackEntry.Submit(Guid.NewGuid(), _projectId, target, FeedbackCategory.General, rating, null, null, null, null),
                CancellationToken.None);
        }

        var result = await CreateHandler().Handle(new GetFeedbackByProjectQuery(_projectId, 2, 2, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(2);
    }
}
