using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Application.UpdateMemberRole;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Tests;

public sealed class UpdateMemberRoleHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_UpdatesRole()
    {
        var projectId = ProjectId.New();
        var member = Member.Invite(projectId, "user-1", Role.Owner);
        var members = new FakeMemberRepository(member, Member.Invite(projectId, "user-2", Role.Owner));
        var handler = new UpdateMemberRoleHandler(members, new FakeUnitOfWork());

        var result = await handler.Handle(new UpdateMemberRoleCommand(projectId.Value, member.Id.Value, Role.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(Role.Admin);
    }

    [Fact]
    public async Task Handle_CannotDemoteLastOwner_ReturnsError()
    {
        var projectId = ProjectId.New();
        var member = Member.Invite(projectId, "user-1", Role.Owner);
        var members = new FakeMemberRepository(member);
        var handler = new UpdateMemberRoleHandler(members, new FakeUnitOfWork());

        var result = await handler.Handle(new UpdateMemberRoleCommand(projectId.Value, member.Id.Value, Role.Admin), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.member.last_owner");
    }

    private sealed class FakeMemberRepository(params Member[] members) : IMemberRepository
    {
        private readonly List<Member> _members = [.. members];

        public Task<bool> ExistsByExternalUserAsync(ProjectId projectId, string externalUserId, CancellationToken cancellationToken)
            => Task.FromResult(_members.Any(member => member.ProjectId == projectId && member.ExternalUserId == externalUserId));

        public Task AddAsync(Member member, CancellationToken cancellationToken)
        {
            _members.Add(member);
            return Task.CompletedTask;
        }

        public Task<Member?> GetByIdAsync(MemberId memberId, CancellationToken cancellationToken)
            => Task.FromResult(_members.FirstOrDefault(member => member.Id == memberId));

        public Task<int> CountOwnersAsync(ProjectId projectId, CancellationToken cancellationToken)
            => Task.FromResult(_members.Count(member => member.ProjectId == projectId && member.Role == Role.Owner));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
