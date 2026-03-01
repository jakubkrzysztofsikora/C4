using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Project;
using C4.Modules.Identity.Domain.User;

namespace C4.Modules.Identity.Application.Ports;

public interface ITokenService
{
    string GenerateToken(UserId userId, string email, string displayName, IReadOnlyList<ProjectMembership> memberships);
}

public sealed record ProjectMembership(ProjectId ProjectId, Role Role);
