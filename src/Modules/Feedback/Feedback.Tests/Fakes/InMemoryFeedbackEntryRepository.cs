using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;

namespace C4.Modules.Feedback.Tests.Fakes;

internal sealed class InMemoryFeedbackEntryRepository : IFeedbackEntryRepository
{
    private readonly List<FeedbackEntry> _entries = [];

    public IReadOnlyCollection<FeedbackEntry> All => _entries.AsReadOnly();

    public Task AddAsync(FeedbackEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<FeedbackEntry?> FindByIdAsync(FeedbackEntryId id, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyCollection<FeedbackEntry>> FindByTargetAsync(
        FeedbackTargetType targetType, Guid targetId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FeedbackEntry> result = _entries
            .Where(e => e.Target.TargetType == targetType && e.Target.TargetId == targetId)
            .OrderByDescending(e => e.SubmittedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectAsync(
        Guid projectId, int skip, int take, FeedbackCategory? category, CancellationToken cancellationToken)
    {
        IEnumerable<FeedbackEntry> query = _entries;

        if (category.HasValue)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        IReadOnlyCollection<FeedbackEntry> result = query
            .OrderByDescending(e => e.SubmittedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> CountByTargetAsync(FeedbackTargetType targetType, Guid targetId, CancellationToken cancellationToken)
    {
        int count = _entries.Count(e => e.Target.TargetType == targetType && e.Target.TargetId == targetId);
        return Task.FromResult(count);
    }

    public Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectForAggregationAsync(
        Guid projectId, FeedbackCategory? category, CancellationToken cancellationToken)
    {
        IEnumerable<FeedbackEntry> query = _entries;

        if (category.HasValue)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        IReadOnlyCollection<FeedbackEntry> result = query
            .OrderByDescending(e => e.SubmittedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> CountByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.Count);

    public Task<int> CountWithCorrectionsAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.Count(e =>
            e.NodeCorrection is not null || e.EdgeCorrection is not null || e.ClassificationCorrection is not null));

    public Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectAndDateRangeAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FeedbackEntry> result = _entries
            .Where(e => e.SubmittedAtUtc >= fromUtc && e.SubmittedAtUtc <= toUtc)
            .OrderByDescending(e => e.SubmittedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }
}
