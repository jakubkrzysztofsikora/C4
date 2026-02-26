using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;

namespace C4.Modules.Feedback.Application.Ports;

public interface ILearningInsightRepository
{
    Task AddAsync(LearningInsight insight, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LearningInsight>> FindByProjectAndCategoryAsync(Guid projectId, FeedbackCategory category, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LearningInsight>> GetActiveByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task UpdateAsync(LearningInsight insight, CancellationToken cancellationToken);
}
