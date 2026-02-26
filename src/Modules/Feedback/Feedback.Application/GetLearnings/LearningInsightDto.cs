using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;

namespace C4.Modules.Feedback.Application.GetLearnings;

public sealed record LearningInsightDto(
    Guid Id,
    FeedbackCategory Category,
    InsightType InsightType,
    string Description,
    double Confidence,
    int FeedbackCount,
    DateTime CreatedAtUtc);
