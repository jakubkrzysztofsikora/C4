using FluentValidation;

namespace C4.Modules.Discovery.Application.ConnectAzureSubscription;

public sealed class ConnectAzureSubscriptionValidator : AbstractValidator<ConnectAzureSubscriptionCommand>
{
    public ConnectAzureSubscriptionValidator()
    {
        RuleFor(command => command.ExternalSubscriptionId).NotEmpty().MaximumLength(100);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(150);
    }
}
