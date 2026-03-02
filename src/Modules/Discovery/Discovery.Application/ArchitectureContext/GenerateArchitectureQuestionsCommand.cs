using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.ArchitectureContext;

public sealed record GenerateArchitectureQuestionsCommand(
    Guid ProjectId,
    int? CurrentResourceCount = null,
    bool Force = false) : IRequest<Result<GenerateArchitectureQuestionsResponse>>;

public sealed record GenerateArchitectureQuestionsResponse(
    Guid ProjectId,
    bool Regenerated,
    IReadOnlyCollection<ArchitectureContextQuestionDto> Questions);

public sealed class GenerateArchitectureQuestionsHandler(
    IArchitectureContextRepository repository,
    IArchitectureQuestionGenerator questionGenerator,
    IProjectAuthorizationService authorizationService,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateArchitectureQuestionsCommand, Result<GenerateArchitectureQuestionsResponse>>
{
    public async Task<Result<GenerateArchitectureQuestionsResponse>> Handle(GenerateArchitectureQuestionsCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (authCheck.IsFailure)
            return Result<GenerateArchitectureQuestionsResponse>.Failure(authCheck.Error);

        var profile = await repository.GetProfileAsync(request.ProjectId, cancellationToken)
            ?? new ProjectArchitectureProfileRecord(
                request.ProjectId,
                "",
                "",
                "",
                "",
                "",
                IsApproved: false,
                LastUpdatedAtUtc: DateTime.UtcNow);

        bool shouldRegenerate = request.Force || profile.LastResourceCount is null || request.CurrentResourceCount is null;
        if (!shouldRegenerate && request.CurrentResourceCount is int currentCount && profile.LastResourceCount is int previousCount && previousCount > 0)
        {
            var delta = Math.Abs(currentCount - previousCount) / (double)previousCount;
            shouldRegenerate = delta >= 0.15;
        }

        var existingQuestions = await repository.GetQuestionsAsync(request.ProjectId, cancellationToken);
        if (!shouldRegenerate && existingQuestions.Count > 0)
        {
            return Result<GenerateArchitectureQuestionsResponse>.Success(
                new GenerateArchitectureQuestionsResponse(
                    request.ProjectId,
                    Regenerated: false,
                    existingQuestions.OrderBy(q => q.CreatedAtUtc)
                        .Select(q => new ArchitectureContextQuestionDto(q.Id, q.Question, q.Answer, q.IsApproved))
                        .ToArray()));
        }

        string summary = $"""
            Project description: {profile.ProjectDescription}
            System boundaries: {profile.SystemBoundaries}
            Core domains: {profile.CoreDomains}
            External dependencies: {profile.ExternalDependencies}
            Data sensitivity: {profile.DataSensitivity}
            """;

        var generated = await questionGenerator.GenerateQuestionsAsync(request.ProjectId, summary, cancellationToken);
        var questions = generated
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(q => new ProjectArchitectureQuestionRecord(
                Guid.NewGuid(),
                request.ProjectId,
                q.Trim(),
                Answer: null,
                IsApproved: false,
                CreatedAtUtc: DateTime.UtcNow,
                AnsweredAtUtc: null))
            .ToArray();

        await repository.ReplaceQuestionsAsync(request.ProjectId, questions, cancellationToken);

        var updatedProfile = profile with
        {
            IsApproved = false,
            LastQuestionGenerationAtUtc = DateTime.UtcNow,
            LastResourceCount = request.CurrentResourceCount ?? profile.LastResourceCount,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
        await repository.UpsertProfileAsync(updatedProfile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GenerateArchitectureQuestionsResponse>.Success(
            new GenerateArchitectureQuestionsResponse(
                request.ProjectId,
                Regenerated: true,
                questions.Select(q => new ArchitectureContextQuestionDto(q.Id, q.Question, q.Answer, q.IsApproved)).ToArray()));
    }
}
