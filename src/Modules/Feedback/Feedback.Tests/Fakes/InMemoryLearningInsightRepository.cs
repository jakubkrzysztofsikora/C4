using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;

namespace C4.Modules.Feedback.Tests.Fakes;

internal sealed class InMemoryLearningInsightRepository : ILearningInsightRepository
{
    private readonly List<LearningInsight> _insights = [];

    public IReadOnlyCollection<LearningInsight> All => _insights.AsReadOnly();

    public Task AddAsync(LearningInsight insight, CancellationToken cancellationToken)
    {
        _insights.Add(insight);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<LearningInsight>> FindByProjectAndCategoryAsync(
        Guid projectId, FeedbackCategory category, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<LearningInsight> result = _insights
            .Where(i => i.ProjectId == projectId && i.Category == category && !i.IsExpired)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<LearningInsight>> GetActiveByProjectAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<LearningInsight> result = _insights
            .Where(i => i.ProjectId == projectId && !i.IsExpired)
            .ToList();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(LearningInsight insight, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
