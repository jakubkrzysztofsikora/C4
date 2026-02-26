using FluentValidation;

namespace C4.Modules.Identity.Application.RegisterUser;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8);
    }
}
