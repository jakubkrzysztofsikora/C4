using C4.Modules.Feedback.Domain.Events;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.Learning;

public sealed class LearningInsight : AggregateRoot<LearningInsightId>
{
    private LearningInsight(
        LearningInsightId id,
        Guid projectId,
        FeedbackCategory category,
        InsightType insightType,
        string description,
        double confidence,
        int feedbackCount) : base(id)
    {
        ProjectId = projectId;
        Category = category;
        InsightType = insightType;
        Description = description;
        Confidence = confidence;
        FeedbackCount = feedbackCount;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = DateTime.UtcNow.AddDays(30);
    }

    public Guid ProjectId { get; }
    public FeedbackCategory Category { get; }
    public InsightType InsightType { get; }
    public string Description { get; }
    public double Confidence { get; }
    public int FeedbackCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime ExpiresAtUtc { get; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;

    public static LearningInsight Aggregate(
        Guid projectId,
        FeedbackCategory category,
        InsightType insightType,
        string description,
        double confidence,
        int feedbackCount)
    {
        var insight = new LearningInsight(
            LearningInsightId.New(), projectId, category, insightType, description, confidence, feedbackCount);

        insight.Raise(new LearningsAggregatedEvent(insight.Id, projectId, category));

        return insight;
    }
}
