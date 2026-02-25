using FluentValidation;

namespace C4.Modules.Discovery.Application.DetectDrift;

public sealed class DetectDriftCommandValidator : AbstractValidator<DetectDriftCommand>
{
    private static readonly string[] SupportedFormats = ["bicep", "terraform", "tf"];

    public DetectDriftCommandValidator()
    {
        RuleFor(command => command.IacContent).NotEmpty();
        RuleFor(command => command.Format)
            .NotEmpty()
            .Must(format => SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase))
            .WithMessage(command => $"Unsupported IaC format '{command.Format}'. Supported formats: {string.Join(", ", SupportedFormats)}.");
    }
}
