using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;

namespace C4.Modules.Feedback.Application.Ports;

public interface ILearningAggregator
{
    Task<IReadOnlyCollection<LearningInsight>> AggregateAsync(
        Guid projectId,
        IReadOnlyCollection<FeedbackEntry> entries,
        CancellationToken cancellationToken);
}
