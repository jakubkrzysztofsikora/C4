using C4.Modules.Feedback.Application.SubmitFeedback;
using C4.Modules.Feedback.Domain.Corrections;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Tests.Builders;
using C4.Modules.Feedback.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace C4.Modules.Feedback.Tests.SubmitFeedback;

public sealed class SubmitFeedbackHandlerTests
{
    private readonly InMemoryFeedbackEntryRepository _repository = new();
    private readonly InMemoryFeedbackUnitOfWork _unitOfWork = new();
    private readonly NullMediator _mediator = new();

    private SubmitFeedbackHandler CreateHandler() =>
        new(_repository, _unitOfWork, _mediator, NullLogger<SubmitFeedbackHandler>.Instance);

    [Fact]
    public async Task Handle_ValidFeedback_CreatesFeedbackEntry()
    {
        var handler = CreateHandler();
        var command = new FeedbackCommandBuilder().Build();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FeedbackEntryId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ValidFeedback_PersistsFeedbackEntry()
    {
        var handler = CreateHandler();
        var command = new FeedbackCommandBuilder().Build();

        await handler.Handle(command, CancellationToken.None);

        _repository.All.Should().ContainSingle();
        _unitOfWork.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidFeedback_PreservesAllFields()
    {
        var handler = CreateHandler();
        var userId = Guid.NewGuid();
        var command = new FeedbackCommandBuilder()
            .WithUserId(userId)
            .WithCategory(FeedbackCategory.ThreatAssessment)
            .WithRating(5)
            .WithComment("great analysis")
            .Build();

        await handler.Handle(command, CancellationToken.None);

        var persisted = _repository.All.Single();
        persisted.UserId.Should().Be(userId);
        persisted.Category.Should().Be(FeedbackCategory.ThreatAssessment);
        persisted.Rating.Score.Should().Be(5);
        persisted.Comment.Should().Be("great analysis");
    }

    [Fact]
    public async Task Handle_WithNodeCorrection_IncludesCorrection()
    {
        var handler = CreateHandler();
        var correction = new NodeCorrection("OldName", "NewName", "Container", "Component", null, null, null, null);
        var command = new FeedbackCommandBuilder().WithNodeCorrection(correction).Build();

        await handler.Handle(command, CancellationToken.None);

        _repository.All.Single().NodeCorrection.Should().Be(correction);
    }

    [Fact]
    public async Task Handle_WithEdgeCorrection_IncludesCorrection()
    {
        var handler = CreateHandler();
        var correction = new EdgeCorrection("http", "https", true);
        var command = new FeedbackCommandBuilder().WithEdgeCorrection(correction).Build();

        await handler.Handle(command, CancellationToken.None);

        _repository.All.Single().EdgeCorrection.Should().Be(correction);
    }

    [Fact]
    public async Task Handle_WithClassificationCorrection_IncludesCorrection()
    {
        var handler = CreateHandler();
        var correction = new ClassificationCorrection(
            "Microsoft.Web/sites", "Web App", "App Service", "external", "app", "Container", "Container", true, true);
        var command = new FeedbackCommandBuilder().WithClassificationCorrection(correction).Build();

        await handler.Handle(command, CancellationToken.None);

        _repository.All.Single().ClassificationCorrection.Should().Be(correction);
    }

    [Fact]
    public async Task Handle_InvalidRating_ReturnsFailure()
    {
        var handler = CreateHandler();
        var command = new FeedbackCommandBuilder().WithRating(0).Build();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("feedback.invalid_rating");
    }

    [Fact]
    public async Task Handle_InvalidRating_DoesNotPersist()
    {
        var handler = CreateHandler();
        var command = new FeedbackCommandBuilder().WithRating(6).Build();

        await handler.Handle(command, CancellationToken.None);

        _repository.All.Should().BeEmpty();
        _unitOfWork.SaveChangesCount.Should().Be(0);
    }
}
