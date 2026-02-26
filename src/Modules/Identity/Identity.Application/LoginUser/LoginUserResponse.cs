namespace C4.Modules.Identity.Application.LoginUser;

public sealed record LoginUserResponse(Guid UserId, string Email, string DisplayName, string Token);
