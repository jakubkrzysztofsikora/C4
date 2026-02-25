using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Application.GetServiceHealth;

public sealed class GetServiceHealthHandler(ITelemetryRepository repository)
    : IRequestHandler<GetServiceHealthQuery, Result<GetServiceHealthResponse>>
{
    public async Task<Result<GetServiceHealthResponse>> Handle(GetServiceHealthQuery request, CancellationToken cancellationToken)
    {
        var health = await repository.GetServiceHealthAsync(request.ProjectId, request.Service.Trim(), cancellationToken);
        if (health is null) return Result<GetServiceHealthResponse>.Failure(TelemetryErrors.HealthNotFound(request.ProjectId, request.Service));

        return Result<GetServiceHealthResponse>.Success(new GetServiceHealthResponse(health.ProjectId, health.Service, health.Score, health.Status.ToString()));
    }
}
