using C4.Modules.Feedback.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetEvalMetrics;

public sealed class GetEvalMetricsHandler(IFeedbackEntryRepository feedbackRepository)
    : IRequestHandler<GetEvalMetricsQuery, Result<EvalMetricsDto>>
{
    private const int WeeksToAnalyze = 12;

    public async Task<Result<EvalMetricsDto>> Handle(GetEvalMetricsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await feedbackRepository.CountByProjectAsync(request.ProjectId, cancellationToken);

        if (totalCount == 0)
        {
            return Result<EvalMetricsDto>.Success(new EvalMetricsDto(0, 0, 0, [], []));
        }

        var correctionCount = await feedbackRepository.CountWithCorrectionsAsync(request.ProjectId, cancellationToken);
        var correctionRate = (double)correctionCount / totalCount;

        var now = DateTime.UtcNow;
        var fromDate = now.AddDays(-7 * WeeksToAnalyze);
        var entries = await feedbackRepository.GetByProjectAndDateRangeAsync(request.ProjectId, fromDate, now, cancellationToken);

        var overallAverage = entries.Count > 0
            ? entries.Average(e => e.Rating.Score)
            : 0.0;

        var weeklyTrends = entries
            .GroupBy(e => GetWeekStart(e.SubmittedAtUtc))
            .OrderBy(g => g.Key)
            .Select(g => new WeeklyRatingTrend(g.Key, g.Average(e => e.Rating.Score), g.Count()))
            .ToList();

        var perPlugin = entries
            .GroupBy(e => e.Category.ToString())
            .Select(g => new PluginMetrics(
                g.Key,
                g.Count(),
                g.Average(e => e.Rating.Score),
                g.Count(e => e.NodeCorrection is not null || e.EdgeCorrection is not null || e.ClassificationCorrection is not null)))
            .OrderByDescending(p => p.FeedbackCount)
            .ToList();

        return Result<EvalMetricsDto>.Success(new EvalMetricsDto(
            totalCount,
            overallAverage,
            correctionRate,
            weeklyTrends,
            perPlugin));
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}
