using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.GetSubscription;

public sealed record GetSubscriptionQuery : IRequest<Result<GetSubscriptionResponse>>;

public sealed record GetSubscriptionResponse(Guid SubscriptionId, string ExternalSubscriptionId, string DisplayName);
