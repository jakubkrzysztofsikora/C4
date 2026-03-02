namespace C4.Shared.Kernel.Contracts;

public interface IProjectArchitectureContextProvider
{
    Task<ProjectArchitectureContextDto?> GetActiveContextAsync(Guid projectId, CancellationToken cancellationToken);
}

public sealed record ProjectArchitectureContextDto(
    string ProjectDescription,
    string SystemBoundaries,
    string CoreDomains,
    string ExternalDependencies,
    string DataSensitivity,
    IReadOnlyCollection<ArchitectureClarifyingQuestionDto> ApprovedQuestions);

public sealed record ArchitectureClarifyingQuestionDto(Guid Id, string Question, string Answer);
