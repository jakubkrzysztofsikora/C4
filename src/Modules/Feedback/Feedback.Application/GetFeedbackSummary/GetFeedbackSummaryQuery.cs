using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetFeedbackSummary;

public sealed record GetFeedbackSummaryQuery(Guid ProjectId) : IRequest<Result<FeedbackSummaryDto>>;
