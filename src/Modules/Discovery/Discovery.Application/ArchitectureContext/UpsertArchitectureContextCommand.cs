using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.ArchitectureContext;

public sealed record UpsertArchitectureContextCommand(
    Guid ProjectId,
    string ProjectDescription,
    string SystemBoundaries,
    string CoreDomains,
    string ExternalDependencies,
    string DataSensitivity) : IRequest<Result<GetArchitectureContextResponse>>;

public sealed class UpsertArchitectureContextHandler(
    IArchitectureContextRepository repository,
    IProjectAuthorizationService authorizationService,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertArchitectureContextCommand, Result<GetArchitectureContextResponse>>
{
    public async Task<Result<GetArchitectureContextResponse>> Handle(UpsertArchitectureContextCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (authCheck.IsFailure)
            return Result<GetArchitectureContextResponse>.Failure(authCheck.Error);

        var current = await repository.GetProfileAsync(request.ProjectId, cancellationToken);

        var profile = new ProjectArchitectureProfileRecord(
            request.ProjectId,
            request.ProjectDescription.Trim(),
            request.SystemBoundaries.Trim(),
            request.CoreDomains.Trim(),
            request.ExternalDependencies.Trim(),
            request.DataSensitivity.Trim(),
            IsApproved: current?.IsApproved ?? false,
            LastUpdatedAtUtc: DateTime.UtcNow,
            LastQuestionGenerationAtUtc: current?.LastQuestionGenerationAtUtc,
            LastResourceCount: current?.LastResourceCount);

        await repository.UpsertProfileAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var questions = await repository.GetQuestionsAsync(request.ProjectId, cancellationToken);

        return Result<GetArchitectureContextResponse>.Success(
            new GetArchitectureContextResponse(
                request.ProjectId,
                profile.ProjectDescription,
                profile.SystemBoundaries,
                profile.CoreDomains,
                profile.ExternalDependencies,
                profile.DataSensitivity,
                profile.IsApproved,
                questions
                    .OrderBy(q => q.CreatedAtUtc)
                    .Select(q => new ArchitectureContextQuestionDto(q.Id, q.Question, q.Answer, q.IsApproved))
                    .ToArray()));
    }
}
