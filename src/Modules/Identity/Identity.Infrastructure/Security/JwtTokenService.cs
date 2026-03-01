using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.User;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace C4.Modules.Identity.Infrastructure.Security;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateToken(UserId userId, string email, string displayName, IReadOnlyList<ProjectMembership> memberships)
    {
        string signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey must be configured. Set via environment variable 'Jwt__SigningKey'.");
        string issuer = configuration["Jwt:Issuer"] ?? "c4-api";
        string audience = configuration["Jwt:Audience"] ?? "c4-web";
        int expirationMinutes = configuration.GetValue<int>("Jwt:ExpirationMinutes", 1440);

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(signingKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("display_name", displayName),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        ];

        foreach (var membership in memberships)
        {
            claims.Add(new Claim("project_role", $"{membership.ProjectId.Value}:{membership.Role}"));
        }

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
