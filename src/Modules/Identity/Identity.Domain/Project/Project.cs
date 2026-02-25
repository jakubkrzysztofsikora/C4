using C4.Modules.Identity.Domain.Organization;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Project;

public sealed class Project : AggregateRoot<ProjectId>
{
    private Project(ProjectId id, OrganizationId organizationId, string name) : base(id)
    {
        OrganizationId = organizationId;
        Name = name;
    }

    public OrganizationId OrganizationId { get; }

    public string Name { get; }

    public static Result<Project> Create(OrganizationId organizationId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Project>.Failure(Domain.Errors.IdentityErrors.EmptyName("Project name"));
        }

        return Result<Project>.Success(new Project(ProjectId.New(), organizationId, name.Trim()));
    }
}
