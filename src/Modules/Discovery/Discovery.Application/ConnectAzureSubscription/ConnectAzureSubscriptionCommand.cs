using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ConnectAzureSubscription;

public sealed record ConnectAzureSubscriptionCommand(
    string ExternalSubscriptionId,
    string DisplayName,
    string? GitRepoUrl,
    string? GitPatToken)
    : IRequest<Result<ConnectAzureSubscriptionResponse>>;
