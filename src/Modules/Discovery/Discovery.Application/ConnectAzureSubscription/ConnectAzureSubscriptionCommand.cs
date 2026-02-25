using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ConnectAzureSubscription;

public sealed record ConnectAzureSubscriptionCommand(string ExternalSubscriptionId, string DisplayName)
    : IRequest<Result<ConnectAzureSubscriptionResponse>>;
