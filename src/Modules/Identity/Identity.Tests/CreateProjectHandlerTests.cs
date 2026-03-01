using C4.Modules.Identity.Application.CreateProject;
using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Organization;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Tests;

public sealed class CreateProjectHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProject()
    {
        var organization = Organization.Create("Acme").Value;
        var organizationRepository = new FakeOrganizationRepository(organization);
        var projectRepository = new FakeProjectRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateProjectHandler(organizationRepository, projectRepository, new FakeMemberRepository(), new FakeCurrentUserService(), unitOfWork);

        var result = await handler.Handle(new CreateProjectCommand(organization.Id.Value, "Portal"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Portal");
        unitOfWork.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_OrganizationNotFound_ReturnsError()
    {
        var handler = new CreateProjectHandler(new FakeOrganizationRepository(), new FakeProjectRepository(), new FakeMemberRepository(), new FakeCurrentUserService(), new FakeUnitOfWork());

        var result = await handler.Handle(new CreateProjectCommand(Guid.NewGuid(), "Portal"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.organization.not_found");
    }

    [Fact]
    public async Task Handle_DuplicateProjectName_ReturnsError()
    {
        var organization = Organization.Create("Acme").Value;
        var organizationRepository = new FakeOrganizationRepository(organization);
        var projectRepository = new FakeProjectRepository();
        await projectRepository.AddAsync(Project.Create(organization.Id, "Portal").Value, CancellationToken.None);
        var handler = new CreateProjectHandler(organizationRepository, projectRepository, new FakeMemberRepository(), new FakeCurrentUserService(), new FakeUnitOfWork());

        var result = await handler.Handle(new CreateProjectCommand(organization.Id.Value, "Portal"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.project.duplicate_name");
    }

    private sealed class FakeOrganizationRepository(params Organization[] organizations) : IOrganizationRepository
    {
        private readonly Dictionary<OrganizationId, Organization> _organizations = organizations.ToDictionary(organization => organization.Id);

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
            => Task.FromResult(_organizations.Values.Any(organization => string.Equals(organization.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
        {
            _organizations[organization.Id] = organization;
            return Task.CompletedTask;
        }

        public Task<Organization?> GetByIdAsync(OrganizationId organizationId, CancellationToken cancellationToken)
        {
            _organizations.TryGetValue(organizationId, out var organization);
            return Task.FromResult(organization);
        }

        public Task<Organization?> GetFirstAsync(CancellationToken cancellationToken)
            => Task.FromResult(_organizations.Values.FirstOrDefault());
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        private readonly List<Project> _projects = [];

        public Task<bool> ExistsByNameAsync(OrganizationId organizationId, string projectName, CancellationToken cancellationToken)
            => Task.FromResult(
                _projects.Any(project =>
                    project.OrganizationId == organizationId &&
                    string.Equals(project.Name, projectName, StringComparison.OrdinalIgnoreCase)));

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

    [Fact]
    public async Task Handle_ValidCommand_AddsCreatorAsOwnerMember()
    {
        var organization = Organization.Create("Acme").Value;
        var organizationRepository = new FakeOrganizationRepository(organization);
        var projectRepository = new FakeProjectRepository();
        var memberRepository = new FakeMemberRepository();
        var handler = new CreateProjectHandler(organizationRepository, projectRepository, memberRepository, new FakeCurrentUserService(), new FakeUnitOfWork());

        await handler.Handle(new CreateProjectCommand(organization.Id.Value, "Portal"), CancellationToken.None);

        memberRepository.Members.Should().ContainSingle();
        memberRepository.Members[0].Role.Should().Be(Role.Owner);
    }

    private sealed class FakeMemberRepository : IMemberRepository
    {
        public List<Domain.Member.Member> Members { get; } = [];

        public Task<bool> ExistsByExternalUserAsync(ProjectId projectId, string externalUserId, CancellationToken cancellationToken)
            => Task.FromResult(Members.Any(m => m.ProjectId == projectId && m.ExternalUserId == externalUserId));

        public Task AddAsync(Domain.Member.Member member, CancellationToken cancellationToken)
        {
            Members.Add(member);
            return Task.CompletedTask;
        }

        public Task<Domain.Member.Member?> GetByIdAsync(MemberId memberId, CancellationToken cancellationToken)
            => Task.FromResult(Members.FirstOrDefault(m => m.Id == memberId));

        public Task<int> CountOwnersAsync(ProjectId projectId, CancellationToken cancellationToken)
            => Task.FromResult(Members.Count(m => m.ProjectId == projectId && m.Role == Role.Owner));

        public Task<Domain.Member.Member?> GetByProjectAndUserAsync(ProjectId projectId, string externalUserId, CancellationToken cancellationToken)
            => Task.FromResult(Members.FirstOrDefault(m => m.ProjectId == projectId && m.ExternalUserId == externalUserId));

        public Task<IReadOnlyList<Domain.Member.Member>> GetByExternalUserIdAsync(string externalUserId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Domain.Member.Member>>(Members.Where(m => m.ExternalUserId == externalUserId).ToList());
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string Email => "test@example.com";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }
}
