using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Application.IngestTelemetry;

public sealed class IngestTelemetryHandler(ITelemetryRepository repository, IMediator mediator, IUnitOfWork unitOfWork)
    : IRequestHandler<IngestTelemetryCommand, Result<IngestTelemetryResponse>>
{
    public async Task<Result<IngestTelemetryResponse>> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
    {
        var metric = new MetricDataPoint(request.ProjectId, request.Service.Trim(), request.Value, DateTime.UtcNow);
        await repository.AddMetricAsync(metric, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var health = await repository.GetServiceHealthAsync(request.ProjectId, request.Service.Trim(), cancellationToken)
            ?? new ServiceHealth(request.ProjectId, request.Service.Trim(), request.Value, ServiceHealthStatusExtensions.FromScore(request.Value), DateTime.UtcNow);

        await mediator.Publish(
            new TelemetryUpdatedIntegrationEvent(
                request.ProjectId,
                [new TelemetryUpdatedServiceItem(health.Service, health.Score, health.Status.ToString())]),
            cancellationToken);

        return Result<IngestTelemetryResponse>.Success(new IngestTelemetryResponse(health.ProjectId, health.Service, health.Score, health.Status.ToString()));
    }
}
