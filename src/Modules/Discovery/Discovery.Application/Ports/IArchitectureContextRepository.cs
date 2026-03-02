namespace C4.Modules.Discovery.Application.Ports;

public interface IArchitectureContextRepository
{
    Task<ProjectArchitectureProfileRecord?> GetProfileAsync(Guid projectId, CancellationToken cancellationToken);
    Task UpsertProfileAsync(ProjectArchitectureProfileRecord profile, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProjectArchitectureQuestionRecord>> GetQuestionsAsync(Guid projectId, CancellationToken cancellationToken);
    Task ReplaceQuestionsAsync(Guid projectId, IReadOnlyCollection<ProjectArchitectureQuestionRecord> questions, CancellationToken cancellationToken);
    Task UpdateQuestionAnswerAsync(Guid projectId, Guid questionId, string answer, CancellationToken cancellationToken);
}

public sealed record ProjectArchitectureProfileRecord(
    Guid ProjectId,
    string ProjectDescription,
    string SystemBoundaries,
    string CoreDomains,
    string ExternalDependencies,
    string DataSensitivity,
    bool IsApproved,
    DateTime LastUpdatedAtUtc,
    DateTime? LastQuestionGenerationAtUtc = null,
    int? LastResourceCount = null);

public sealed record ProjectArchitectureQuestionRecord(
    Guid Id,
    Guid ProjectId,
    string Question,
    string? Answer,
    bool IsApproved,
    DateTime CreatedAtUtc,
    DateTime? AnsweredAtUtc);

public interface IArchitectureQuestionGenerator
{
    Task<IReadOnlyCollection<string>> GenerateQuestionsAsync(Guid projectId, string contextSummary, CancellationToken cancellationToken);
}
