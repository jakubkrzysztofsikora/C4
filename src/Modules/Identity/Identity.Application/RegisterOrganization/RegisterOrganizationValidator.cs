using FluentValidation;

namespace C4.Modules.Identity.Application.RegisterOrganization;

public sealed class RegisterOrganizationValidator : AbstractValidator<RegisterOrganizationCommand>
{
    public RegisterOrganizationValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
    }
}
