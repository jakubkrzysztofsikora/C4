using C4.Modules.Identity.Domain.Organization;

namespace C4.Modules.Identity.Application.Ports;

public interface IOrganizationRepository
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Organization organization, CancellationToken cancellationToken);

    Task<Organization?> GetByIdAsync(OrganizationId organizationId, CancellationToken cancellationToken);
}
