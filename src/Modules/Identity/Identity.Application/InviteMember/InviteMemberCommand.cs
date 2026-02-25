using C4.Modules.Identity.Domain.Member;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.InviteMember;

public sealed record InviteMemberCommand(Guid ProjectId, string ExternalUserId, Role Role) : IRequest<Result<InviteMemberResponse>>;
