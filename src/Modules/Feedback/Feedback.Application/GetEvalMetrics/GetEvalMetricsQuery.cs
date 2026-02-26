using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetEvalMetrics;

public sealed record GetEvalMetricsQuery(Guid ProjectId) : IRequest<Result<EvalMetricsDto>>;
