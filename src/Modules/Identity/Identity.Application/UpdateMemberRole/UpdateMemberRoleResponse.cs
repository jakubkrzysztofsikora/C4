using C4.Modules.Identity.Domain.Member;

namespace C4.Modules.Identity.Application.UpdateMemberRole;

public sealed record UpdateMemberRoleResponse(Guid ProjectId, Guid MemberId, Role Role);
