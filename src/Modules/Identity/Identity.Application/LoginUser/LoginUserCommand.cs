using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.LoginUser;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<Result<LoginUserResponse>>;
