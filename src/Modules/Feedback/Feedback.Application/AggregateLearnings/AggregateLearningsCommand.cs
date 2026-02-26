using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.AggregateLearnings;

public sealed record AggregateLearningsCommand(Guid ProjectId, FeedbackCategory? Category) : IRequest<Result<AggregateLearningsResponse>>;

public sealed record AggregateLearningsResponse(int InsightsGenerated);
