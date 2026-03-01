using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Modules.Identity.Domain.Organization;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Identity.Application.CreateProject;

public sealed class CreateProjectHandler(
    IOrganizationRepository organizationRepository,
    IProjectRepository projectRepository,
    IMemberRepository memberRepository,
    ICurrentUserService currentUserService,
    [FromKeyedServices("Identity")] IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProjectCommand, Result<CreateProjectResponse>>
{
    public async Task<Result<CreateProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var organizationId = new OrganizationId(request.OrganizationId);
        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Result<CreateProjectResponse>.Failure(IdentityErrors.OrganizationNotFound(request.OrganizationId));
        }

        var projectExists = await projectRepository.ExistsByNameAsync(organization.Id, request.Name.Trim(), cancellationToken);
        var projectResult = organization.CreateProject(request.Name, projectExists);

        if (projectResult.IsFailure)
        {
            return Result<CreateProjectResponse>.Failure(projectResult.Error);
        }

        await projectRepository.AddAsync(projectResult.Value, cancellationToken);

        var ownerMember = Domain.Member.Member.Invite(
            projectResult.Value.Id,
            currentUserService.UserId.ToString(),
            Domain.Member.Role.Owner);
        await memberRepository.AddAsync(ownerMember, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateProjectResponse>.Success(
            new CreateProjectResponse(projectResult.Value.Id.Value, organization.Id.Value, projectResult.Value.Name));
    }
}
