namespace C4.Modules.Feedback.Application.GetEvalMetrics;

public sealed record EvalMetricsDto(
    int TotalFeedbackCount,
    double OverallAverageRating,
    double CorrectionRate,
    IReadOnlyCollection<WeeklyRatingTrend> WeeklyTrends,
    IReadOnlyCollection<PluginMetrics> PerPluginBreakdown);

public sealed record WeeklyRatingTrend(DateTime WeekStartUtc, double AverageRating, int FeedbackCount);

public sealed record PluginMetrics(string Category, int FeedbackCount, double AverageRating, int CorrectionCount);
