using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName) : IRequest<Result<RegisterUserResponse>>;
