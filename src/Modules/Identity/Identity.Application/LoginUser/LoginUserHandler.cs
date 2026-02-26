using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.LoginUser;

internal sealed class LoginUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService
) : IRequestHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    public async Task<Result<LoginUserResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email.Trim(), cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result<LoginUserResponse>.Failure(IdentityErrors.InvalidCredentials());
        }

        string token = tokenService.GenerateToken(user.Id, user.Email, user.DisplayName);

        return Result<LoginUserResponse>.Success(new LoginUserResponse(user.Id.Value, user.Email, user.DisplayName, token));
    }
}
