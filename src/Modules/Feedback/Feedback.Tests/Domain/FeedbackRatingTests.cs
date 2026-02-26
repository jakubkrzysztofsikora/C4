using C4.Modules.Feedback.Domain.FeedbackEntry;

namespace C4.Modules.Feedback.Tests.Domain;

public sealed class FeedbackRatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_ValidScore_ReturnsSuccess(int score)
    {
        var result = FeedbackRating.Create(score);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(score);
    }

    [Fact]
    public void Create_ZeroScore_ReturnsFailure()
    {
        var result = FeedbackRating.Create(0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("feedback.invalid_rating");
    }

    [Fact]
    public void Create_NegativeScore_ReturnsFailure()
    {
        var result = FeedbackRating.Create(-1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("feedback.invalid_rating");
    }

    [Fact]
    public void Create_ScoreAboveFive_ReturnsFailure()
    {
        var result = FeedbackRating.Create(6);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("feedback.invalid_rating");
    }
}
