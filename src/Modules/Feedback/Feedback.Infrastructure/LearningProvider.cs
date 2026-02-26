using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Feedback.Infrastructure;

internal sealed class LearningProvider(ILearningInsightRepository repository) : ILearningProvider
{
    public async Task<IReadOnlyCollection<LearningDto>> GetActiveLearningsAsync(
        Guid projectId, string category, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FeedbackCategory>(category, true, out var feedbackCategory))
        {
            return [];
        }

        var insights = await repository.FindByProjectAndCategoryAsync(projectId, feedbackCategory, cancellationToken);

        return insights
            .Where(i => !i.IsExpired)
            .Select(i => new LearningDto(i.Description, i.Confidence, i.InsightType.ToString()))
            .ToList();
    }
}
