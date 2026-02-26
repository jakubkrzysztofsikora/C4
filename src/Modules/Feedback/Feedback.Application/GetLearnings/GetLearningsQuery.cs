using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetLearnings;

public sealed record GetLearningsQuery(Guid ProjectId, FeedbackCategory? Category) : IRequest<Result<GetLearningsResponse>>;

public sealed record GetLearningsResponse(IReadOnlyCollection<LearningInsightDto> Insights);
