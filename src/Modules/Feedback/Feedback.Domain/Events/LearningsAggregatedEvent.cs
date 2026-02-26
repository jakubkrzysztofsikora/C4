using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;
using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.Events;

public sealed record LearningsAggregatedEvent(LearningInsightId InsightId, Guid ProjectId, FeedbackCategory Category) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
