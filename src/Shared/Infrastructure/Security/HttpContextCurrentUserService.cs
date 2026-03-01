using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using C4.Shared.Kernel;
using Microsoft.AspNetCore.Http;

namespace C4.Shared.Infrastructure.Security;

public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (sub is null || !Guid.TryParse(sub, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            return userId;
        }
    }

    public string Email
    {
        get
        {
            return httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
                ?? throw new UnauthorizedAccessException("User email claim not found.");
        }
    }
}
