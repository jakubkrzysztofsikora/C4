using C4.Modules.Identity.Domain.Organization;
using C4.Modules.Identity.Domain.Project;

namespace C4.Modules.Identity.Application.Ports;

public interface IProjectRepository
{
    Task<bool> ExistsByNameAsync(OrganizationId organizationId, string projectName, CancellationToken cancellationToken);

    Task AddAsync(Project project, CancellationToken cancellationToken);

    Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken);
}
