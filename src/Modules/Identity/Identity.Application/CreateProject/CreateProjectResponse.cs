namespace C4.Modules.Identity.Application.CreateProject;

public sealed record CreateProjectResponse(Guid ProjectId, Guid OrganizationId, string Name);
