using FluentValidation;

namespace C4.Modules.Feedback.Application.SubmitFeedback;

public sealed class SubmitFeedbackValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.TargetId).NotEmpty();
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
        RuleFor(c => c.TargetType).IsInEnum();
        RuleFor(c => c.Category).IsInEnum();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Comment).MaximumLength(2000).When(c => c.Comment is not null);
    }
}
