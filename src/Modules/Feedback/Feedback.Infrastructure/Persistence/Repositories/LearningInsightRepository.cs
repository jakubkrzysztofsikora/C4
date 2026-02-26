using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Feedback.Infrastructure.Persistence.Repositories;

internal sealed class LearningInsightRepository(FeedbackDbContext dbContext) : ILearningInsightRepository
{
    public async Task AddAsync(LearningInsight insight, CancellationToken cancellationToken) =>
        await dbContext.LearningInsights.AddAsync(insight, cancellationToken);

    public async Task<IReadOnlyCollection<LearningInsight>> FindByProjectAndCategoryAsync(
        Guid projectId, FeedbackCategory category, CancellationToken cancellationToken) =>
        await dbContext.LearningInsights
            .Where(i => i.ProjectId == projectId && i.Category == category && i.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.Confidence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<LearningInsight>> GetActiveByProjectAsync(
        Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.LearningInsights
            .Where(i => i.ProjectId == projectId && i.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.Confidence)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(LearningInsight insight, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
