using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Organization;
using C4.Modules.Identity.Domain.Project;
using C4.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Identity.Infrastructure.Repositories;

public sealed class ProjectRepository(IdentityDbContext dbContext) : IProjectRepository
{
    public Task<bool> ExistsByNameAsync(OrganizationId organizationId, string projectName, CancellationToken cancellationToken) =>
        dbContext.Projects.AnyAsync(project => project.OrganizationId == organizationId && project.Name == projectName, cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken) =>
        await dbContext.Projects.AddAsync(project, cancellationToken);

    public Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken) =>
        dbContext.Projects.FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken) =>
        await dbContext.Projects.Where(project => project.OrganizationId == organizationId).ToListAsync(cancellationToken);
}
