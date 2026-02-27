using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Organization;
using C4.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Identity.Infrastructure.Repositories;

public sealed class OrganizationRepository(IdentityDbContext dbContext) : IOrganizationRepository
{
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Organizations.AnyAsync(organization => organization.Name == name, cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken) =>
        await dbContext.Organizations.AddAsync(organization, cancellationToken);

    public Task<Organization?> GetByIdAsync(OrganizationId organizationId, CancellationToken cancellationToken) =>
        dbContext.Organizations.FirstOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken);

    public Task<Organization?> GetFirstAsync(CancellationToken cancellationToken) =>
        dbContext.Organizations.FirstOrDefaultAsync(cancellationToken);
}
