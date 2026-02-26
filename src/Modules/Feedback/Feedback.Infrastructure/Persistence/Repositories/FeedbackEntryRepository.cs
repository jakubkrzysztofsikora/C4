using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Feedback.Infrastructure.Persistence.Repositories;

internal sealed class FeedbackEntryRepository(FeedbackDbContext dbContext) : IFeedbackEntryRepository
{
    public async Task AddAsync(FeedbackEntry entry, CancellationToken cancellationToken) =>
        await dbContext.FeedbackEntries.AddAsync(entry, cancellationToken);

    public async Task<FeedbackEntry?> FindByIdAsync(FeedbackEntryId id, CancellationToken cancellationToken) =>
        await dbContext.FeedbackEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<FeedbackEntry>> FindByTargetAsync(
        FeedbackTargetType targetType, Guid targetId, CancellationToken cancellationToken) =>
        await dbContext.FeedbackEntries
            .Where(e => e.Target.TargetType == targetType && e.Target.TargetId == targetId)
            .OrderByDescending(e => e.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectAsync(
        Guid projectId, int skip, int take, FeedbackCategory? category, CancellationToken cancellationToken)
    {
        IQueryable<FeedbackEntry> query = dbContext.FeedbackEntries
            .Where(e => e.ProjectId == projectId);

        if (category.HasValue)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        return await query
            .OrderByDescending(e => e.SubmittedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByTargetAsync(
        FeedbackTargetType targetType, Guid targetId, CancellationToken cancellationToken) =>
        await dbContext.FeedbackEntries
            .CountAsync(e => e.Target.TargetType == targetType && e.Target.TargetId == targetId, cancellationToken);

    public async Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectForAggregationAsync(
        Guid projectId, FeedbackCategory? category, CancellationToken cancellationToken)
    {
        IQueryable<FeedbackEntry> query = dbContext.FeedbackEntries
            .Where(e => e.ProjectId == projectId);

        if (category.HasValue)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        return await query
            .OrderByDescending(e => e.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.FeedbackEntries.CountAsync(e => e.ProjectId == projectId, cancellationToken);

    public async Task<int> CountWithCorrectionsAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.FeedbackEntries
            .CountAsync(e => e.ProjectId == projectId && (e.NodeCorrection != null || e.EdgeCorrection != null || e.ClassificationCorrection != null), cancellationToken);

    public async Task<IReadOnlyCollection<FeedbackEntry>> GetByProjectAndDateRangeAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        await dbContext.FeedbackEntries
            .Where(e => e.ProjectId == projectId && e.SubmittedAtUtc >= fromUtc && e.SubmittedAtUtc <= toUtc)
            .OrderByDescending(e => e.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
}
