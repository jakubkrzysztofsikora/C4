using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ArchitectureContext;

public sealed record GetArchitectureContextQuery(Guid ProjectId) : IRequest<Result<GetArchitectureContextResponse>>;

public sealed record GetArchitectureContextResponse(
    Guid ProjectId,
    string ProjectDescription,
    string SystemBoundaries,
    string CoreDomains,
    string ExternalDependencies,
    string DataSensitivity,
    bool IsApproved,
    IReadOnlyCollection<ArchitectureContextQuestionDto> Questions);

public sealed record ArchitectureContextQuestionDto(Guid Id, string Question, string? Answer, bool IsApproved);

public sealed class GetArchitectureContextHandler(
    IArchitectureContextRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetArchitectureContextQuery, Result<GetArchitectureContextResponse>>
{
    public async Task<Result<GetArchitectureContextResponse>> Handle(GetArchitectureContextQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (authCheck.IsFailure)
            return Result<GetArchitectureContextResponse>.Failure(authCheck.Error);

        var profile = await repository.GetProfileAsync(request.ProjectId, cancellationToken)
                      ?? new ProjectArchitectureProfileRecord(
                          request.ProjectId,
                          ProjectDescription: "",
                          SystemBoundaries: "",
                          CoreDomains: "",
                          ExternalDependencies: "",
                          DataSensitivity: "",
                          IsApproved: false,
                          LastUpdatedAtUtc: DateTime.UtcNow);
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
