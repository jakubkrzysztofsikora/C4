using C4.Modules.Feedback.Domain.FeedbackEntry;

namespace C4.Modules.Feedback.Application.GetFeedbackByProject;

public sealed record FeedbackEntryDto(
    Guid Id,
    FeedbackTargetType TargetType,
    Guid TargetId,
    FeedbackCategory Category,
    int Rating,
    string? Comment,
    DateTime SubmittedAtUtc,
    Guid UserId);
