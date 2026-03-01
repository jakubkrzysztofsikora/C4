namespace C4.Shared.Kernel;

public interface ICurrentUserService
{
    Guid UserId { get; }

    string Email { get; }
}
