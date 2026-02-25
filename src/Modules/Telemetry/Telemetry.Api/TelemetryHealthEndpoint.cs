using C4.Shared.Infrastructure.Endpoints;

namespace C4.Modules.Telemetry.Api;

public sealed class TelemetryHealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/telemetry/health", () => Results.Ok(new { module = "Telemetry", status = "ok" }));
    }
}
