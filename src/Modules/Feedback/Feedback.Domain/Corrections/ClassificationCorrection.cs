namespace C4.Modules.Feedback.Domain.Corrections;

public sealed record ClassificationCorrection(
    string ArmResourceType,
    string? OriginalFriendlyName,
    string? CorrectedFriendlyName,
    string? OriginalServiceType,
    string? CorrectedServiceType,
    string? OriginalC4Level,
    string? CorrectedC4Level,
    bool? OriginalIncludeInDiagram,
    bool? CorrectedIncludeInDiagram);
