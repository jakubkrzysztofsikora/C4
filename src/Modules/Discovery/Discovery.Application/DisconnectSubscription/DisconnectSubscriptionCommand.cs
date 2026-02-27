using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DisconnectSubscription;

public sealed record DisconnectSubscriptionCommand : IRequest<Result<DisconnectSubscriptionResponse>>;
