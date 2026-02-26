namespace C4.Modules.Identity.Application.RegisterUser;

public sealed record RegisterUserResponse(Guid UserId, string Email, string DisplayName, string Token);
