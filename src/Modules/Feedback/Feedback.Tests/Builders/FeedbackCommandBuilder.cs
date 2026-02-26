using C4.Modules.Feedback.Application.SubmitFeedback;
using C4.Modules.Feedback.Domain.Corrections;
using C4.Modules.Feedback.Domain.FeedbackEntry;

namespace C4.Modules.Feedback.Tests.Builders;

internal sealed class FeedbackCommandBuilder
{
    private Guid _projectId = Guid.NewGuid();
    private FeedbackTargetType _targetType = FeedbackTargetType.GraphNode;
    private Guid _targetId = Guid.NewGuid();
    private FeedbackCategory _category = FeedbackCategory.NodeClassification;
    private int _rating = 4;
    private string? _comment;
    private NodeCorrection? _nodeCorrection;
    private EdgeCorrection? _edgeCorrection;
    private ClassificationCorrection? _classificationCorrection;
    private Guid _userId = Guid.NewGuid();

    public FeedbackCommandBuilder WithRating(int rating) { _rating = rating; return this; }
    public FeedbackCommandBuilder WithComment(string comment) { _comment = comment; return this; }
    public FeedbackCommandBuilder WithCategory(FeedbackCategory category) { _category = category; return this; }
    public FeedbackCommandBuilder WithNodeCorrection(NodeCorrection correction) { _nodeCorrection = correction; return this; }
    public FeedbackCommandBuilder WithEdgeCorrection(EdgeCorrection correction) { _edgeCorrection = correction; return this; }
    public FeedbackCommandBuilder WithClassificationCorrection(ClassificationCorrection correction) { _classificationCorrection = correction; return this; }
    public FeedbackCommandBuilder WithUserId(Guid userId) { _userId = userId; return this; }
    public FeedbackCommandBuilder WithProjectId(Guid projectId) { _projectId = projectId; return this; }
    public FeedbackCommandBuilder WithTargetType(FeedbackTargetType targetType) { _targetType = targetType; return this; }
    public FeedbackCommandBuilder WithTargetId(Guid targetId) { _targetId = targetId; return this; }

    public SubmitFeedbackCommand Build() => new(
        _projectId, _targetType, _targetId, _category, _rating,
        _comment, _nodeCorrection, _edgeCorrection, _classificationCorrection, _userId);
}
