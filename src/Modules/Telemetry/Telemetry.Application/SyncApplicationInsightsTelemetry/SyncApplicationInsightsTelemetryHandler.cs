using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Application.SyncApplicationInsightsTelemetry;

public sealed class SyncApplicationInsightsTelemetryHandler(
    IApplicationInsightsClient applicationInsightsClient,
    ITelemetryRepository telemetryRepository,
    IMediator mediator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SyncApplicationInsightsTelemetryCommand, Result<SyncApplicationInsightsTelemetryResponse>>
{
    public async Task<Result<SyncApplicationInsightsTelemetryResponse>> Handle(SyncApplicationInsightsTelemetryCommand request, CancellationToken cancellationToken)
    {
        var records = await applicationInsightsClient.QueryServiceHealthAsync(request.ProjectId, TimeSpan.FromMinutes(request.LookbackMinutes), cancellationToken);

        foreach (var record in records)
        {
            await telemetryRepository.AddMetricAsync(new MetricDataPoint(request.ProjectId, record.Service, record.Score, record.ObservedAtUtc), cancellationToken);
        }

        var updates = records
            .Select(record => new TelemetryUpdatedServiceItem(record.Service, record.Score, ServiceHealthStatusExtensions.FromScore(record.Score).ToString()))
            .ToArray();

        await mediator.Publish(new TelemetryUpdatedIntegrationEvent(request.ProjectId, updates), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SyncApplicationInsightsTelemetryResponse>.Success(new SyncApplicationInsightsTelemetryResponse(request.ProjectId, records.Count));
    }
}
