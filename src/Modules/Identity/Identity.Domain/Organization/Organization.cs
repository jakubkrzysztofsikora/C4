using C4.Modules.Identity.Domain.Events;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Organization;

public sealed class Organization : AggregateRoot<OrganizationId>
{
    private readonly List<ProjectId> _projects = [];

    private Organization(OrganizationId id, string name) : base(id)
    {
        Name = name;
    }

    public string Name { get; }

    public IReadOnlyCollection<ProjectId> Projects => _projects.AsReadOnly();

    public static Result<Organization> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Organization>.Failure(Domain.Errors.IdentityErrors.EmptyName("Organization name"));
        }

        var organization = new Organization(OrganizationId.New(), name.Trim());
        organization.Raise(new OrganizationCreatedEvent(organization.Id, organization.Name));
        return Result<Organization>.Success(organization);
    }

    public Result<Project.Project> CreateProject(string projectName, bool isProjectNameTaken)
    {
        if (isProjectNameTaken)
        {
            return Result<Project.Project>.Failure(Domain.Errors.IdentityErrors.DuplicateProjectName(projectName.Trim()));
        }

        return Project.Project.Create(Id, projectName)
            .Map(project =>
            {
                _projects.Add(project.Id);
                Raise(new ProjectCreatedEvent(project.Id, Id, project.Name));
                return project;
            });
    }
}
