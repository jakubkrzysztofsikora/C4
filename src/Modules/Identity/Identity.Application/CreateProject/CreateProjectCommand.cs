using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.CreateProject;

public sealed record CreateProjectCommand(Guid OrganizationId, string Name) : IRequest<Result<CreateProjectResponse>>;
