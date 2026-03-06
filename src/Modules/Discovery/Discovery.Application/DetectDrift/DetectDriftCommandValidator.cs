using FluentValidation;

namespace C4.Modules.Discovery.Application.DetectDrift;

public sealed class DetectDriftCommandValidator : AbstractValidator<DetectDriftCommand>
{
    private static readonly string[] SupportedFormats = ["bicep", "terraform", "tf"];

    public DetectDriftCommandValidator()
    {
        RuleFor(command => command)
            .Must(command =>
                command.UseRepositories
                || !string.IsNullOrWhiteSpace(command.IacContent))
            .WithMessage("Either enable repository mode or provide inline IaC content.");

        When(command => !string.IsNullOrWhiteSpace(command.IacContent), () =>
        {
            RuleFor(command => command.Format)
                .NotEmpty()
                .Must(format => format is not null && SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase))
                .WithMessage(command => $"Unsupported IaC format '{command.Format}'. Supported formats: {string.Join(", ", SupportedFormats)}.");
        });
    }
}
