using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.GetOrganization;

public sealed class GetOrganizationHandler(
    IOrganizationRepository organizationRepository,
    IProjectRepository projectRepository,
    IMemberRepository memberRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetOrganizationQuery, Result<GetOrganizationResponse>>
{
    public async Task<Result<GetOrganizationResponse>> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetFirstAsync(cancellationToken);
        if (organization is null)
        {
            return Result<GetOrganizationResponse>.Failure(IdentityErrors.OrganizationNotFound(Guid.Empty));
        }

        var userMemberships = await memberRepository.GetByExternalUserIdAsync(
            currentUserService.UserId.ToString(),
            cancellationToken);
        var memberProjectIds = userMemberships.Select(m => m.ProjectId).ToHashSet();

        var projects = await projectRepository.GetByOrganizationIdAsync(organization.Id, cancellationToken);
        var projectDtos = projects
            .Where(p => memberProjectIds.Contains(p.Id))
            .Select(p => new OrganizationProjectDto(p.Id.Value, p.Name))
            .ToList();

        return Result<GetOrganizationResponse>.Success(
            new GetOrganizationResponse(organization.Id.Value, organization.Name, projectDtos));
    }
}
