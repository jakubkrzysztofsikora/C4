using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Identity.Application.InviteMember;

public sealed class InviteMemberHandler(
    IProjectRepository projectRepository,
    IMemberRepository memberRepository,
    IProjectAuthorizationService authorizationService,
    ICurrentUserService currentUserService,
    [FromKeyedServices("Identity")] IUnitOfWork unitOfWork) : IRequestHandler<InviteMemberCommand, Result<InviteMemberResponse>>
{
    public async Task<Result<InviteMemberResponse>> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var ownerCheck = await authorizationService.AuthorizeOwnerAsync(request.ProjectId, cancellationToken);
        if (ownerCheck.IsFailure)
            return Result<InviteMemberResponse>.Failure(ownerCheck.Error);

        var projectId = new ProjectId(request.ProjectId);
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return Result<InviteMemberResponse>.Failure(IdentityErrors.ProjectNotFound(request.ProjectId));
        }

        var normalizedExternalUserId = request.ExternalUserId.Trim();

        if (normalizedExternalUserId == currentUserService.UserId.ToString())
        {
            return Result<InviteMemberResponse>.Failure(IdentityErrors.CannotInviteSelf());
        }

        if (await memberRepository.ExistsByExternalUserAsync(projectId, normalizedExternalUserId, cancellationToken))
        {
            return Result<InviteMemberResponse>.Failure(IdentityErrors.MemberAlreadyExists(normalizedExternalUserId));
        }

        var member = Member.Invite(projectId, normalizedExternalUserId, request.Role);

        await memberRepository.AddAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<InviteMemberResponse>.Success(
            new InviteMemberResponse(member.Id.Value, projectId.Value, member.ExternalUserId, member.Role));
    }
}
