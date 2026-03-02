using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.ArchitectureContext;

public sealed record AnswerArchitectureQuestionCommand(Guid ProjectId, Guid QuestionId, string Answer) : IRequest<Result<bool>>;

public sealed class AnswerArchitectureQuestionHandler(
    IArchitectureContextRepository repository,
    IProjectAuthorizationService authorizationService,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork)
    : IRequestHandler<AnswerArchitectureQuestionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AnswerArchitectureQuestionCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (authCheck.IsFailure)
            return Result<bool>.Failure(authCheck.Error);

        await repository.UpdateQuestionAnswerAsync(request.ProjectId, request.QuestionId, request.Answer.Trim(), cancellationToken);
        var profile = await repository.GetProfileAsync(request.ProjectId, cancellationToken);
        if (profile is not null)
        {
            await repository.UpsertProfileAsync(profile with
            {
                IsApproved = false,
                LastUpdatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
