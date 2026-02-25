using C4.Modules.Identity.Domain.Member;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.UpdateMemberRole;

public sealed record UpdateMemberRoleCommand(Guid ProjectId, Guid MemberId, Role Role) : IRequest<Result<UpdateMemberRoleResponse>>;
