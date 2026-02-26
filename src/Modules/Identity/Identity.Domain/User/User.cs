using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.User;

public sealed class User : Entity<UserId>
{
    private User(UserId id, string email, string passwordHash, string displayName) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
    }

    public string Email { get; }

    public string PasswordHash { get; }

    public string DisplayName { get; }

    public static User Create(string email, string passwordHash, string displayName) =>
        new(UserId.New(), email.Trim(), passwordHash, displayName.Trim());

    public bool VerifyPassword(string passwordHash) => PasswordHash == passwordHash;
}
