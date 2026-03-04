using C4.Modules.Telemetry.Application.Ports;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Telemetry.Api.Endpoints;

public sealed class GetTelemetryStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/telemetry/status", async (
            Guid projectId,
            IAppInsightsConfigStore configStore,
            ITelemetryQueryService telemetryQueryService,
            IProjectAuthorizationService authorizationService,
            CancellationToken ct) =>
        {
            var auth = await authorizationService.AuthorizeAsync(projectId, ct);
            if (!auth.IsSuccess) return Results.Forbid();

            var targets = await configStore.GetTargetsAsync(projectId, ct);
            var health = await telemetryQueryService.GetServiceHealthSummariesAsync(projectId, ct);
            var dependencies = await telemetryQueryService.GetDependencySummariesAsync(projectId, ct);

            var knownHealth = health.Count(item => item.TelemetryStatus.Equals("known", StringComparison.OrdinalIgnoreCase));
            var knownDependencies = dependencies.Count(item => item.TelemetryStatus.Equals("known", StringComparison.OrdinalIgnoreCase));

            return Results.Ok(new TelemetryStatusResponse(
                projectId,
                targets.Count,
                targets.Count > 0,
                knownHealth,
                knownDependencies,
                knownHealth > 0 || knownDependencies > 0,
                DateTime.UtcNow));
        }).RequireAuthorization();
    }

    public sealed record TelemetryStatusResponse(
        Guid ProjectId,
        int TargetCount,
        bool HasTargets,
        int KnownHealthServices,
        int KnownDependencies,
        bool HasKnownTelemetry,
        DateTime GeneratedAtUtc);
}
