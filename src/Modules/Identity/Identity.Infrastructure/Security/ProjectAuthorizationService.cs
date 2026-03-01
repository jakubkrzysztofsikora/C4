using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Infrastructure.Security;

internal sealed class ProjectAuthorizationService(
    ICurrentUserService currentUserService,
    IMemberRepository memberRepository) : IProjectAuthorizationService
{
    private static readonly Error NotAMember = new("authorization.not_a_member", "User is not a member of this project.");
    private static readonly Error NotAnOwner = new("authorization.not_an_owner", "User is not an owner of this project.");

    public async Task<Result<bool>> AuthorizeAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByProjectAndUserAsync(
            new ProjectId(projectId),
            currentUserService.UserId.ToString(),
            cancellationToken);

        return member is null
            ? Result<bool>.Failure(NotAMember)
            : Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AuthorizeOwnerAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByProjectAndUserAsync(
            new ProjectId(projectId),
            currentUserService.UserId.ToString(),
            cancellationToken);

        if (member is null)
            return Result<bool>.Failure(NotAMember);

        return member.Role != Role.Owner
            ? Result<bool>.Failure(NotAnOwner)
            : Result<bool>.Success(true);
    }
}
