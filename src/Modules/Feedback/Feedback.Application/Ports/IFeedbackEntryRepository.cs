using C4.Modules.Feedback.Domain.FeedbackEntry;

namespace C4.Modules.Feedback.Application.Ports;

public interface IFeedbackEntryRepository
{
    Task AddAsync(FeedbackEntry entry, CancellationToken cancellationToken);
    Task<FeedbackEntry?> FindByIdAsync(FeedbackEntryId id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackEntry>> FindByTargetAsync(FeedbackTargetType targetType, Guid targetId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectAsync(Guid projectId, int skip, int take, FeedbackCategory? category, CancellationToken cancellationToken);
    Task<int> CountByTargetAsync(FeedbackTargetType targetType, Guid targetId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectForAggregationAsync(Guid projectId, FeedbackCategory? category, CancellationToken cancellationToken);
    Task<int> CountByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<int> CountWithCorrectionsAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectAndDateRangeAsync(Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
