namespace C4.Modules.Feedback.Domain.Corrections;

public sealed record EdgeCorrection(
    string? OriginalRelationship,
    string? CorrectedRelationship,
    bool ShouldExist);
