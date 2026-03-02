using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Discovery.Infrastructure;

public sealed class ProjectArchitectureContextProvider(IArchitectureContextRepository repository) : IProjectArchitectureContextProvider
{
    public async Task<ProjectArchitectureContextDto?> GetActiveContextAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(projectId, cancellationToken);
        if (profile is null || !profile.IsApproved)
            return null;

        var questions = await repository.GetQuestionsAsync(projectId, cancellationToken);
        var approved = questions
            .Where(q => q.IsApproved && !string.IsNullOrWhiteSpace(q.Answer))
            .Select(q => new ArchitectureClarifyingQuestionDto(q.Id, q.Question, q.Answer!))
            .ToArray();

        return new ProjectArchitectureContextDto(
            profile.ProjectDescription,
            profile.SystemBoundaries,
            profile.CoreDomains,
            profile.ExternalDependencies,
            profile.DataSensitivity,
            approved);
    }
}
