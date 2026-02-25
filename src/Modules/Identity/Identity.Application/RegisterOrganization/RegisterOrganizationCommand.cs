using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.RegisterOrganization;

public sealed record RegisterOrganizationCommand(string Name) : IRequest<Result<RegisterOrganizationResponse>>;
