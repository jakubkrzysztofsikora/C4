using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Telemetry.Application.SyncApplicationInsightsTelemetry;

public sealed class SyncApplicationInsightsTelemetryHandler(
    IApplicationInsightsClient applicationInsightsClient,
    ITelemetryRepository telemetryRepository,
    IMediator mediator,
    [FromKeyedServices("Telemetry")] IUnitOfWork unitOfWork,
    IProjectAuthorizationService authorizationService,
    ILogger<SyncApplicationInsightsTelemetryHandler> logger)
    : IRequestHandler<SyncApplicationInsightsTelemetryCommand, Result<SyncApplicationInsightsTelemetryResponse>>
{
    public async Task<Result<SyncApplicationInsightsTelemetryResponse>> Handle(SyncApplicationInsightsTelemetryCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<SyncApplicationInsightsTelemetryResponse>.Failure(authCheck.Error);

        var lookback = TimeSpan.FromMinutes(request.LookbackMinutes);

        IReadOnlyCollection<ApplicationInsightsHealthRecord> healthRecords;
        try
        {
            healthRecords = await applicationInsightsClient.QueryServiceHealthAsync(request.ProjectId, lookback, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "App Insights health telemetry sync failed for project {ProjectId}", request.ProjectId);
            healthRecords = [];
        }

        IReadOnlyCollection<ApplicationInsightsDependencyRecord> dependencyRecords;
        try
        {
            dependencyRecords = await applicationInsightsClient.QueryDependencyHealthAsync(request.ProjectId, lookback, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "App Insights dependency telemetry sync failed for project {ProjectId}", request.ProjectId);
            dependencyRecords = [];
        }

        foreach (var record in healthRecords)
        {
            await telemetryRepository.AddMetricAsync(new MetricDataPoint(request.ProjectId, record.Service, record.Score, record.ObservedAtUtc), cancellationToken);
        }

        var updates = healthRecords
            .Select(record => new TelemetryUpdatedServiceItem(record.Service, record.Score, ServiceHealthStatusExtensions.FromScore(record.Score).ToString()))
            .ToArray();

        await mediator.Publish(new TelemetryUpdatedIntegrationEvent(request.ProjectId, updates), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SyncApplicationInsightsTelemetryResponse>.Success(
            new SyncApplicationInsightsTelemetryResponse(
                request.ProjectId,
                healthRecords.Count + dependencyRecords.Count,
                healthRecords.Count,
                dependencyRecords.Count));
    }
}
