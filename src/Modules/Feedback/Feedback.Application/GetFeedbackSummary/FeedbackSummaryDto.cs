namespace C4.Modules.Feedback.Application.GetFeedbackSummary;

public sealed record FeedbackSummaryDto(
    int TotalCount,
    double AverageRating,
    IReadOnlyCollection<CategoryBreakdownItem> CategoryBreakdown);

public sealed record CategoryBreakdownItem(string Category, int Count, double AverageRating);
