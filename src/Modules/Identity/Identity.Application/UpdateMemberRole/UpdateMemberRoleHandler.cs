using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.UpdateMemberRole;

public sealed class UpdateMemberRoleHandler(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMemberRoleCommand, Result<UpdateMemberRoleResponse>>
{
    public async Task<Result<UpdateMemberRoleResponse>> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var projectId = new ProjectId(request.ProjectId);
        var memberId = new MemberId(request.MemberId);

        var member = await memberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.ProjectId != projectId)
        {
            return Result<UpdateMemberRoleResponse>.Failure(IdentityErrors.MemberNotFound(request.MemberId));
        }

        if (member.Role == Role.Owner && request.Role != Role.Owner)
        {
            var ownerCount = await memberRepository.CountOwnersAsync(projectId, cancellationToken);
            if (ownerCount <= 1)
            {
                return Result<UpdateMemberRoleResponse>.Failure(IdentityErrors.CannotDemoteLastOwner(request.ProjectId));
            }
        }

        member.UpdateRole(request.Role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateMemberRoleResponse>.Success(new UpdateMemberRoleResponse(projectId.Value, memberId.Value, member.Role));
    }
}
