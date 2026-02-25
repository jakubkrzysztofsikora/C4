using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;

namespace C4.Modules.Identity.Application.Ports;

public interface IMemberRepository
{
    Task<bool> ExistsByExternalUserAsync(ProjectId projectId, string externalUserId, CancellationToken cancellationToken);

    Task AddAsync(Member member, CancellationToken cancellationToken);

    Task<Member?> GetByIdAsync(MemberId memberId, CancellationToken cancellationToken);

    Task<int> CountOwnersAsync(ProjectId projectId, CancellationToken cancellationToken);
}
