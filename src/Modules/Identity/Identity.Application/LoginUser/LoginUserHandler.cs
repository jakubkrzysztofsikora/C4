using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.LoginUser;

internal sealed class LoginUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IMemberRepository memberRepository
) : IRequestHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    public async Task<Result<LoginUserResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email.Trim(), cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result<LoginUserResponse>.Failure(IdentityErrors.InvalidCredentials());
        }

        var members = await memberRepository.GetByExternalUserIdAsync(user.Id.Value.ToString(), cancellationToken);
        var memberships = members.Select(m => new ProjectMembership(m.ProjectId, m.Role)).ToList();

        string token = tokenService.GenerateToken(user.Id, user.Email, user.DisplayName, memberships);

        return Result<LoginUserResponse>.Success(new LoginUserResponse(user.Id.Value, user.Email, user.DisplayName, token));
    }
}
