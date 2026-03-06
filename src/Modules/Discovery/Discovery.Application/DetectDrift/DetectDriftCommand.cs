using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DetectDrift;

public sealed record DetectDriftCommand(
    Guid SubscriptionId,
    string? IacContent,
    string? Format,
    bool UseRepositories = true,
    string? Environment = null) : IRequest<Result<DetectDriftResponse>>;

public sealed record DetectDriftResponse(Guid SubscriptionId, int DriftedCount, int InSyncCount);
