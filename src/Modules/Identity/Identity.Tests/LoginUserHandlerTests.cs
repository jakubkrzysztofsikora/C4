using C4.Modules.Identity.Application.LoginUser;
using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;
using C4.Modules.Identity.Domain.User;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Tests;

public sealed class LoginUserHandlerTests
{
    private const string ValidEmail = "test@example.com";
    private const string ValidPassword = "SecurePass1!";
    private const string DisplayName = "Test User";
    private const string GeneratedToken = "jwt_token_value";

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var userRepository = new RegisterUserHandlerTests.FakeUserRepository();
        await SeedUserAsync(userRepository);
        var handler = CreateHandler(userRepository);

        var result = await handler.Handle(
            new LoginUserCommand(ValidEmail, ValidPassword),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(ValidEmail);
        result.Value.DisplayName.Should().Be(DisplayName);
        result.Value.Token.Should().Be(GeneratedToken);
    }

    [Fact]
    public async Task Handle_ValidCredentials_IncludesMembershipsInToken()
    {
        var userRepository = new RegisterUserHandlerTests.FakeUserRepository();
        await SeedUserAsync(userRepository);
        var projectId = ProjectId.New();
        var tokenService = new CapturingTokenService();
        var memberRepository = new FakeMemberRepository();
        var user = (await userRepository.GetByEmailAsync(ValidEmail, CancellationToken.None))!;
        memberRepository.AddMember(Member.Invite(projectId, user.Id.Value.ToString(), Role.Admin));
        var handler = CreateHandler(userRepository, tokenService: tokenService, memberRepository: memberRepository);

        await handler.Handle(
            new LoginUserCommand(ValidEmail, ValidPassword),
            CancellationToken.None);

        tokenService.LastMemberships.Should().HaveCount(1);
        tokenService.LastMemberships![0].ProjectId.Should().Be(projectId);
        tokenService.LastMemberships[0].Role.Should().Be(Role.Admin);
    }

    [Fact]
    public async Task Handle_UserWithNoMemberships_PassesEmptyMemberships()
    {
        var userRepository = new RegisterUserHandlerTests.FakeUserRepository();
        await SeedUserAsync(userRepository);
        var tokenService = new CapturingTokenService();
        var handler = CreateHandler(userRepository, tokenService: tokenService);

        await handler.Handle(
            new LoginUserCommand(ValidEmail, ValidPassword),
            CancellationToken.None);

        tokenService.LastMemberships.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidCredentialsError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new LoginUserCommand("unknown@example.com", ValidPassword),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.user.invalid_credentials");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentialsError()
    {
        var userRepository = new RegisterUserHandlerTests.FakeUserRepository();
        await SeedUserAsync(userRepository);
        var passwordHasher = new RegisterUserHandlerTests.FakePasswordHasher();
        var handler = CreateHandler(userRepository, passwordHasher);

        var result = await handler.Handle(
            new LoginUserCommand(ValidEmail, "WrongPassword!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.user.invalid_credentials");
    }

    private static async Task SeedUserAsync(RegisterUserHandlerTests.FakeUserRepository repository)
    {
        var hasher = new RegisterUserHandlerTests.FakePasswordHasher();
        var user = User.Create(ValidEmail, hasher.Hash(ValidPassword), DisplayName);
        await repository.AddAsync(user, CancellationToken.None);
    }

    private static LoginUserHandler CreateHandler(
        RegisterUserHandlerTests.FakeUserRepository? userRepository = null,
        RegisterUserHandlerTests.FakePasswordHasher? passwordHasher = null,
        ITokenService? tokenService = null,
        FakeMemberRepository? memberRepository = null) =>
        new(
            userRepository ?? new RegisterUserHandlerTests.FakeUserRepository(),
            passwordHasher ?? new RegisterUserHandlerTests.FakePasswordHasher(),
            tokenService ?? new RegisterUserHandlerTests.FakeTokenService(),
            memberRepository ?? new FakeMemberRepository());

    private sealed class CapturingTokenService : ITokenService
    {
        public IReadOnlyList<ProjectMembership>? LastMemberships { get; private set; }

        public string GenerateToken(UserId userId, string email, string displayName, IReadOnlyList<ProjectMembership> memberships)
        {
            LastMemberships = memberships;
            return GeneratedToken;
        }
    }

    private sealed class FakeMemberRepository : IMemberRepository
    {
        private readonly List<Member> _members = [];

        public void AddMember(Member member) => _members.Add(member);

        public Task<bool> ExistsByExternalUserAsync(ProjectId projectId, string externalUserId, CancellationToken cancellationToken)
            => Task.FromResult(_members.Any(m => m.ProjectId == projectId && m.ExternalUserId == externalUserId));

        public Task AddAsync(Member member, CancellationToken cancellationToken)
        {
            _members.Add(member);
            return Task.CompletedTask;
        }

        public Task<Member?> GetByIdAsync(MemberId memberId, CancellationToken cancellationToken)
            => Task.FromResult(_members.FirstOrDefault(m => m.Id == memberId));

        public Task<int> CountOwnersAsync(ProjectId projectId, CancellationToken cancellationToken)
            => Task.FromResult(_members.Count(m => m.ProjectId == projectId && m.Role == Role.Owner));

        public Task<Member?> GetByProjectAndUserAsync(ProjectId projectId, string externalUserId, CancellationToken cancellationToken)
            => Task.FromResult(_members.FirstOrDefault(m => m.ProjectId == projectId && m.ExternalUserId == externalUserId));

        public Task<IReadOnlyList<Member>> GetByExternalUserIdAsync(string externalUserId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Member>>(_members.Where(m => m.ExternalUserId == externalUserId).ToList());
    }
}
