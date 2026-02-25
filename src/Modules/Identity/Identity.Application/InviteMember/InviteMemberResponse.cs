using C4.Modules.Identity.Domain.Member;

namespace C4.Modules.Identity.Application.InviteMember;

public sealed record InviteMemberResponse(Guid MemberId, Guid ProjectId, string ExternalUserId, Role Role);
