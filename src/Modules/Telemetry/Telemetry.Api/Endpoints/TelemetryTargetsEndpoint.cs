using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;

namespace C4.Modules.Telemetry.Api.Endpoints;

public sealed class TelemetryTargetsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/telemetry/targets/v2", async (
            Guid projectId,
            ITelemetryTargetStore targetStore,
            IProjectAuthorizationService authorizationService,
            CancellationToken ct) =>
        {
            var auth = await authorizationService.AuthorizeAsync(projectId, ct);
            if (!auth.IsSuccess) return Results.Forbid();

            IReadOnlyCollection<TelemetryTarget> targets = await targetStore.GetTargetsAsync(projectId, ct);
            var response = new TelemetryTargetsV2Response(
                projectId,
                targets.Select(t => new TelemetryTargetV2Dto(t.Id, t.Provider.ToString(), t.AuthMode.ToString(), t.ConnectionMetadata)).ToArray());

            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapGet("/api/projects/{projectId:guid}/telemetry/targets", async (
            Guid projectId,
            IAppInsightsConfigStore configStore,
            IProjectAuthorizationService authorizationService,
            CancellationToken ct) =>
        {
            var auth = await authorizationService.AuthorizeAsync(projectId, ct);
            if (!auth.IsSuccess) return Results.Forbid();

            var targets = await configStore.GetTargetsAsync(projectId, ct);
            var response = new TelemetryTargetsResponse(
                projectId,
                targets.Select(appId => new TelemetryTargetDto(appId, appId, "app-insights")).ToArray());

            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapPost("/api/projects/{projectId:guid}/telemetry/targets", async (
            Guid projectId,
            UpsertTelemetryTargetRequest request,
            IAppInsightsConfigStore configStore,
            IProjectAuthorizationService authorizationService,
            CancellationToken ct) =>
        {
            var auth = await authorizationService.AuthorizeAsync(projectId, ct);
            if (!auth.IsSuccess) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.AppId))
                return Results.BadRequest(new { error = "appId is required." });

            await configStore.StoreAsync(
                projectId,
                request.AppId.Trim(),
                request.InstrumentationKey ?? string.Empty,
                request.ApiKey ?? string.Empty,
                ct);

            var targets = await configStore.GetTargetsAsync(projectId, ct);
            var response = new TelemetryTargetsResponse(
                projectId,
                targets.Select(appId => new TelemetryTargetDto(appId, appId, "app-insights")).ToArray());

            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapDelete("/api/projects/{projectId:guid}/telemetry/targets/{targetId}", async (
            Guid projectId,
            string targetId,
            IAppInsightsConfigStore configStore,
            IProjectAuthorizationService authorizationService,
            CancellationToken ct) =>
        {
            var auth = await authorizationService.AuthorizeAsync(projectId, ct);
            if (!auth.IsSuccess) return Results.Forbid();

            var existing = await configStore.GetTargetsAsync(projectId, ct);
            if (existing.Count == 0)
                return Results.NoContent();

            var filtered = existing
                .Where(value => !value.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var config = await configStore.GetAsync(projectId, ct);
            await configStore.SaveTargetsAsync(projectId, filtered, config?.InstrumentationKey ?? string.Empty, ct);

            return Results.NoContent();
        }).RequireAuthorization();
    }

    public sealed record UpsertTelemetryTargetRequest(string AppId, string? ApiKey, string? InstrumentationKey);
    public sealed record TelemetryTargetsResponse(Guid ProjectId, IReadOnlyCollection<TelemetryTargetDto> Targets);
    public sealed record TelemetryTargetDto(string TargetId, string AppId, string Source);
    public sealed record TelemetryTargetsV2Response(Guid ProjectId, IReadOnlyCollection<TelemetryTargetV2Dto> Targets);
    public sealed record TelemetryTargetV2Dto(string Id, string Provider, string AuthMode, IReadOnlyDictionary<string, string> ConnectionMetadata);
}
