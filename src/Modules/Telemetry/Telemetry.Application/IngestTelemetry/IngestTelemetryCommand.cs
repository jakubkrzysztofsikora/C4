using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Application.IngestTelemetry;

public sealed record IngestTelemetryCommand(Guid ProjectId, string Service, double Value) : IRequest<Result<IngestTelemetryResponse>>;
public sealed record IngestTelemetryResponse(Guid ProjectId, string Service, double Score, string Status);
