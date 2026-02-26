using C4.Modules.Feedback.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetLearnings;

public sealed class GetLearningsHandler(ILearningInsightRepository repository)
    : IRequestHandler<GetLearningsQuery, Result<GetLearningsResponse>>
{
    public async Task<Result<GetLearningsResponse>> Handle(GetLearningsQuery request, CancellationToken cancellationToken)
    {
        var insights = request.Category.HasValue
            ? await repository.FindByProjectAndCategoryAsync(request.ProjectId, request.Category.Value, cancellationToken)
            : await repository.GetActiveByProjectAsync(request.ProjectId, cancellationToken);

        var activeInsights = insights
            .Where(i => !i.IsExpired)
            .Select(i => new LearningInsightDto(
                i.Id.Value,
                i.Category,
                i.InsightType,
                i.Description,
                i.Confidence,
                i.FeedbackCount,
                i.CreatedAtUtc))
            .ToList();

        return Result<GetLearningsResponse>.Success(new GetLearningsResponse(activeInsights));
    }
}
