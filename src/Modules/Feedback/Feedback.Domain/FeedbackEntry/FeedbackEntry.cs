using C4.Modules.Feedback.Domain.Corrections;
using C4.Modules.Feedback.Domain.Events;
using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.FeedbackEntry;

public sealed class FeedbackEntry : AggregateRoot<FeedbackEntryId>
{
#pragma warning disable CS8618
    private FeedbackEntry() : base(default!) { }
#pragma warning restore CS8618

    private FeedbackEntry(
        FeedbackEntryId id,
        Guid projectId,
        Guid userId,
        FeedbackTarget target,
        FeedbackCategory category,
        FeedbackRating rating,
        string? comment,
        NodeCorrection? nodeCorrection,
        EdgeCorrection? edgeCorrection,
        ClassificationCorrection? classificationCorrection) : base(id)
    {
        ProjectId = projectId;
        UserId = userId;
        Target = target;
        Category = category;
        Rating = rating;
        Comment = comment;
        NodeCorrection = nodeCorrection;
        EdgeCorrection = edgeCorrection;
        ClassificationCorrection = classificationCorrection;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public Guid ProjectId { get; }
    public Guid UserId { get; }
    public FeedbackTarget Target { get; }
    public FeedbackCategory Category { get; }
    public FeedbackRating Rating { get; }
    public string? Comment { get; }
    public NodeCorrection? NodeCorrection { get; }
    public EdgeCorrection? EdgeCorrection { get; }
    public ClassificationCorrection? ClassificationCorrection { get; }
    public DateTime SubmittedAtUtc { get; }

    public static FeedbackEntry Submit(
        Guid userId,
        Guid projectId,
        FeedbackTarget target,
        FeedbackCategory category,
        FeedbackRating rating,
        string? comment,
        NodeCorrection? nodeCorrection,
        EdgeCorrection? edgeCorrection,
        ClassificationCorrection? classificationCorrection)
    {
        FeedbackEntryId id = FeedbackEntryId.New();

        FeedbackEntry entry = new(id, projectId, userId, target, category, rating, comment, nodeCorrection, edgeCorrection, classificationCorrection);

        entry.Raise(new FeedbackSubmittedEvent(id, userId, category));

        return entry;
    }
}
