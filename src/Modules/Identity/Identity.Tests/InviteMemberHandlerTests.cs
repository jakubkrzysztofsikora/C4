using C4.Modules.Identity.Application.InviteMember;
using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Organization;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Tests;

public sealed class InviteMemberHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_AddsMember()
    {
        var organization = Organization.Create("Acme").Value;
        var project = Project.Create(organization.Id, "Portal").Value;
        var handler = new InviteMemberHandler(new FakeProjectRepository(project), new FakeMemberRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(new InviteMemberCommand(project.Id.Value, "user-1", Role.Contributor), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task Handle_AlreadyMember_ReturnsError()
    {
        var organization = Organization.Create("Acme").Value;
        var project = Project.Create(organization.Id, "Portal").Value;
        var members = new FakeMemberRepository();
        await members.AddAsync(Member.Invite(project.Id, "user-1", Role.Viewer), CancellationToken.None);
        var handler = new InviteMemberHandler(new FakeProjectRepository(project), members, new FakeUnitOfWork());

        var result = await handler.Handle(new InviteMemberCommand(project.Id.Value, "user-1", Role.Contributor), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.member.already_exists");
    }

    private sealed class FakeProjectRepository(params Project[] projects) : IProjectRepository
    {
        private readonly List<Project> _projects = [.. projects];

        public Task<bool> ExistsByNameAsync(OrganizationId organizationId, string projectName, CancellationToken cancellationToken)
            => Task.FromResult(_projects.Any(project => project.OrganizationId == organizationId && project.Name == projectName));

        public Task AddAsync(Project project, CancellationToken cancellationToken)
        {
            _projects.Add(project);
            return Task.CompletedTask;
        }

        public Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken)
            => Task.FromResult(_projects.FirstOrDefault(project => project.Id == projectId));

        public Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Project>>(_projects.Where(project => project.OrganizationId == organizationId).ToList());
    }

    private sealed class FakeMemberRepository : IMemberRepository
    {
        private readonly List<Member> _members = [];

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
