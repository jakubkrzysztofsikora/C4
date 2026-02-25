using C4.Modules.Telemetry.Application.SyncApplicationInsightsTelemetry;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Telemetry.Api.Endpoints;

public sealed class SyncApplicationInsightsTelemetryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/telemetry/sync-app-insights", async (Guid projectId, SyncApplicationInsightsTelemetryRequest? request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SyncApplicationInsightsTelemetryCommand(projectId, request?.LookbackMinutes ?? 30), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }

    public sealed record SyncApplicationInsightsTelemetryRequest(int LookbackMinutes = 30);
}
