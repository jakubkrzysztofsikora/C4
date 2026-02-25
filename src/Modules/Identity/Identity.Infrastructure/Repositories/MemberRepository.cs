using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;
using C4.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Identity.Infrastructure.Repositories;

public sealed class MemberRepository(IdentityDbContext dbContext) : IMemberRepository
{
    public Task<bool> ExistsByExternalUserAsync(ProjectId projectId, string externalUserId, CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(member => member.ProjectId == projectId && member.ExternalUserId == externalUserId, cancellationToken);

    public async Task AddAsync(Member member, CancellationToken cancellationToken) =>
        await dbContext.Members.AddAsync(member, cancellationToken);

    public Task<Member?> GetByIdAsync(MemberId memberId, CancellationToken cancellationToken) =>
        dbContext.Members.FirstOrDefaultAsync(member => member.Id == memberId, cancellationToken);

    public Task<int> CountOwnersAsync(ProjectId projectId, CancellationToken cancellationToken) =>
        dbContext.Members.CountAsync(member => member.ProjectId == projectId && member.Role == Role.Owner, cancellationToken);
}
