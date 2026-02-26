using C4.Modules.Feedback.Domain.Errors;
using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.FeedbackEntry;

public sealed record FeedbackRating(int Score)
{
    public static Result<FeedbackRating> Create(int score)
    {
        if (score < 1 || score > 5)
        {
            return Result<FeedbackRating>.Failure(FeedbackErrors.InvalidRating(score));
        }

        return Result<FeedbackRating>.Success(new FeedbackRating(score));
    }
}
