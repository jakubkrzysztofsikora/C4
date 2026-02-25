using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Application.GetServiceHealth;

public sealed record GetServiceHealthQuery(Guid ProjectId, string Service) : IRequest<Result<GetServiceHealthResponse>>;
public sealed record GetServiceHealthResponse(Guid ProjectId, string Service, double Score, string Status);
