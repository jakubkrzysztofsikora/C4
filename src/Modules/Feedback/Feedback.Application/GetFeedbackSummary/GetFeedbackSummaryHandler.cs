using C4.Modules.Feedback.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetFeedbackSummary;

public sealed class GetFeedbackSummaryHandler(
    IFeedbackEntryRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetFeedbackSummaryQuery, Result<FeedbackSummaryDto>>
{
    public async Task<Result<FeedbackSummaryDto>> Handle(GetFeedbackSummaryQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<FeedbackSummaryDto>.Failure(authCheck.Error);

        var entries = await repository.GetByProjectForAggregationAsync(request.ProjectId, null, cancellationToken);

        if (entries.Count == 0)
        {
            return Result<FeedbackSummaryDto>.Success(new FeedbackSummaryDto(0, 0, []));
        }

        double averageRating = entries.Average(e => e.Rating.Score);

        var breakdown = entries
            .GroupBy(e => e.Category)
            .Select(g => new CategoryBreakdownItem(
                g.Key.ToString(),
                g.Count(),
                Math.Round(g.Average(e => e.Rating.Score), 2)))
            .ToList();

        return Result<FeedbackSummaryDto>.Success(new FeedbackSummaryDto(entries.Count, Math.Round(averageRating, 2), breakdown));
    }
}
