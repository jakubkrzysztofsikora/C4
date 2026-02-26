using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;

namespace C4.Modules.Feedback.Tests.Fakes;

internal sealed class FakeLearningAggregator : ILearningAggregator
{
    private readonly List<LearningInsight> _predeterminedInsights = [];

    public void SetInsights(params LearningInsight[] insights) => _predeterminedInsights.AddRange(insights);

    public Task<IReadOnlyCollection<LearningInsight>> AggregateAsync(
        Guid projectId,
        IReadOnlyCollection<FeedbackEntry> entries,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<LearningInsight> result = _predeterminedInsights.ToList();
        return Task.FromResult(result);
    }
}
