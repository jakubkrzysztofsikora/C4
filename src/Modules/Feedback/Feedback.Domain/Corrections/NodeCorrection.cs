namespace C4.Modules.Feedback.Domain.Corrections;

public sealed record NodeCorrection(
    string? OriginalName,
    string? CorrectedName,
    string? OriginalLevel,
    string? CorrectedLevel,
    string? OriginalServiceType,
    string? CorrectedServiceType,
    Guid? OriginalParentId,
    Guid? CorrectedParentId);
