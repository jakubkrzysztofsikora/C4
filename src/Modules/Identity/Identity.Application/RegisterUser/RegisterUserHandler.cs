using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Modules.Identity.Domain.User;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Identity.Application.RegisterUser;

internal sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    [FromKeyedServices("Identity")] IUnitOfWork unitOfWork
) : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email.Trim(), cancellationToken))
        {
            return Result<RegisterUserResponse>.Failure(IdentityErrors.DuplicateEmail(command.Email.Trim()));
        }

        string passwordHash = passwordHasher.Hash(command.Password);
        User user = User.Create(command.Email.Trim(), passwordHash, command.DisplayName.Trim());

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        string token = tokenService.GenerateToken(user.Id, user.Email, user.DisplayName, []);

        return Result<RegisterUserResponse>.Success(new RegisterUserResponse(user.Id.Value, user.Email, user.DisplayName, token));
    }
}
