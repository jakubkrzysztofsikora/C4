using C4.Modules.Telemetry.Application.IngestTelemetry;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Telemetry.Api.Endpoints;

public sealed class IngestTelemetryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/telemetry", async (Guid projectId, IngestTelemetryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new IngestTelemetryCommand(projectId, request.Service, request.Value), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }

    public sealed record IngestTelemetryRequest(string Service, double Value);
}
