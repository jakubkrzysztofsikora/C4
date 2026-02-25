using FluentValidation;

namespace C4.Modules.Identity.Application.UpdateMemberRole;

public sealed class UpdateMemberRoleValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleValidator()
    {
        RuleFor(command => command.ProjectId).NotEmpty();
        RuleFor(command => command.MemberId).NotEmpty();
    }
}
