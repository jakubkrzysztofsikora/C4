using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.GetOrganization;

public sealed record GetOrganizationQuery : IRequest<Result<GetOrganizationResponse>>;

public sealed record OrganizationProjectDto(Guid ProjectId, string Name);

public sealed record GetOrganizationResponse(Guid OrganizationId, string Name, IReadOnlyList<OrganizationProjectDto> Projects);
