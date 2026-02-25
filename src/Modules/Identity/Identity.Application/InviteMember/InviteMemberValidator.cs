using FluentValidation;

namespace C4.Modules.Identity.Application.InviteMember;

public sealed class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberValidator()
    {
        RuleFor(command => command.ProjectId).NotEmpty();
        RuleFor(command => command.ExternalUserId).NotEmpty().MaximumLength(200);
    }
}
