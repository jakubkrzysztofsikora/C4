using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.Errors;

public static class FeedbackErrors
{
    public static Error InvalidRating(int score) => new("feedback.invalid_rating", $"Rating score '{score}' is invalid. Score must be between 1 and 5.");

    public static Error FeedbackNotFound(Guid id) => new("feedback.not_found", $"Feedback entry '{id}' was not found.");
}
