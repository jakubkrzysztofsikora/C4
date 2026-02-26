using C4.Modules.Feedback.Domain.Corrections;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.SubmitFeedback;

public sealed record SubmitFeedbackCommand(
    Guid ProjectId,
    FeedbackTargetType TargetType,
    Guid TargetId,
    FeedbackCategory Category,
    int Rating,
    string? Comment,
    NodeCorrection? NodeCorrection,
    EdgeCorrection? EdgeCorrection,
    ClassificationCorrection? ClassificationCorrection,
    Guid UserId) : IRequest<Result<SubmitFeedbackResponse>>;
