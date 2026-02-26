using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Application.RegisterUser;
using C4.Modules.Identity.Domain.User;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Tests;

public sealed class RegisterUserHandlerTests
{
    private const string ValidEmail = "test@example.com";
    private const string ValidPassword = "SecurePass1!";
    private const string ValidDisplayName = "Test User";
    private const string GeneratedToken = "jwt_token_value";

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndReturnsToken()
    {
        var userRepository = new FakeUserRepository();
        var handler = CreateHandler(userRepository);

        var result = await handler.Handle(
            new RegisterUserCommand(ValidEmail, ValidPassword, ValidDisplayName),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(ValidEmail);
        result.Value.DisplayName.Should().Be(ValidDisplayName);
        result.Value.Token.Should().Be(GeneratedToken);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsError()
    {
        var userRepository = new FakeUserRepository();
        var user = User.Create(ValidEmail, "existing_hash", ValidDisplayName);
        await userRepository.AddAsync(user, CancellationToken.None);
        var handler = CreateHandler(userRepository);

        var result = await handler.Handle(
            new RegisterUserCommand(ValidEmail, ValidPassword, ValidDisplayName),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.user.duplicate_email");
    }

    [Fact]
    public async Task Handle_ValidCommand_HashesPassword()
    {
        var passwordHasher = new FakePasswordHasher();
        var handler = CreateHandler(passwordHasher: passwordHasher);

        await handler.Handle(
            new RegisterUserCommand(ValidEmail, ValidPassword, ValidDisplayName),
            CancellationToken.None);

        passwordHasher.LastHashedPassword.Should().Be(ValidPassword);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesChanges()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(unitOfWork: unitOfWork);

        await handler.Handle(
            new RegisterUserCommand(ValidEmail, ValidPassword, ValidDisplayName),
            CancellationToken.None);

        unitOfWork.SaveChangesCount.Should().Be(1);
    }

    private static RegisterUserHandler CreateHandler(
        FakeUserRepository? userRepository = null,
        FakePasswordHasher? passwordHasher = null,
        FakeUnitOfWork? unitOfWork = null) =>
        new(
            userRepository ?? new FakeUserRepository(),
            passwordHasher ?? new FakePasswordHasher(),
            new FakeTokenService(),
            unitOfWork ?? new FakeUnitOfWork());

    internal sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            _users.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(_users.ContainsKey(email));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users[user.Email] = user;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakePasswordHasher : IPasswordHasher
    {
        public string? LastHashedPassword { get; private set; }

        public string Hash(string password)
        {
            LastHashedPassword = password;
            return $"hashed:{password}";
        }

        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    internal sealed class FakeTokenService : ITokenService
    {
        public string GenerateToken(UserId userId, string email, string displayName) => GeneratedToken;
    }

    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }
}
