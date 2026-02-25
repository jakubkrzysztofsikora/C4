using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Domain.Errors;
using C4.Modules.Identity.Domain.Organization;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Identity.Application.RegisterOrganization;

public sealed class RegisterOrganizationHandler(IOrganizationRepository organizationRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterOrganizationCommand, Result<RegisterOrganizationResponse>>
{
    public async Task<Result<RegisterOrganizationResponse>> Handle(RegisterOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (await organizationRepository.ExistsByNameAsync(request.Name.Trim(), cancellationToken))
        {
            return Result<RegisterOrganizationResponse>.Failure(IdentityErrors.DuplicateOrganizationName(request.Name.Trim()));
        }

        var organizationResult = Organization.Create(request.Name);
        if (organizationResult.IsFailure)
        {
            return Result<RegisterOrganizationResponse>.Failure(organizationResult.Error);
        }

        await organizationRepository.AddAsync(organizationResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegisterOrganizationResponse>.Success(
            new RegisterOrganizationResponse(organizationResult.Value.Id.Value, organizationResult.Value.Name));
    }
}
