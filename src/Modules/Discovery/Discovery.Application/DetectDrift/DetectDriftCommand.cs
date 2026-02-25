using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DetectDrift;

public sealed record DetectDriftCommand(Guid SubscriptionId, string IacContent, string Format) : IRequest<Result<DetectDriftResponse>>;

public sealed record DetectDriftResponse(Guid SubscriptionId, int DriftedCount, int InSyncCount);
