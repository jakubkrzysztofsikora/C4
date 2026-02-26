using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.Events;

public sealed record FeedbackSubmittedEvent(FeedbackEntryId FeedbackEntryId, Guid UserId, FeedbackCategory Category) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
