using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Application.SyncApplicationInsightsTelemetry;

public sealed record SyncApplicationInsightsTelemetryCommand(Guid ProjectId, int LookbackMinutes = 30) : IRequest<Result<SyncApplicationInsightsTelemetryResponse>>;

public sealed record SyncApplicationInsightsTelemetryResponse(
    Guid ProjectId,
    int MetricsIngested,
    int HealthMetricsIngested,
    int DependencyMetricsIngested);
