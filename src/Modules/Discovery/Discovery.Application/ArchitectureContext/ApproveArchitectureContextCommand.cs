using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.ArchitectureContext;

public sealed record ApproveArchitectureContextCommand(Guid ProjectId) : IRequest<Result<bool>>;

public sealed class ApproveArchitectureContextHandler(
    IArchitectureContextRepository repository,
    IProjectAuthorizationService authorizationService,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveArchitectureContextCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ApproveArchitectureContextCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (authCheck.IsFailure)
            return Result<bool>.Failure(authCheck.Error);

        var profile = await repository.GetProfileAsync(request.ProjectId, cancellationToken)
            ?? new ProjectArchitectureProfileRecord(
                request.ProjectId,
                "",
                "",
                "",
                "",
                "",
                IsApproved: true,
                LastUpdatedAtUtc: DateTime.UtcNow);

        await repository.UpsertProfileAsync(profile with
        {
            IsApproved = true,
            LastUpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
