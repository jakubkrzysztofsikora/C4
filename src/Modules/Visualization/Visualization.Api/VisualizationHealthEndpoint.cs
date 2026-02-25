using C4.Shared.Infrastructure.Endpoints;

namespace C4.Modules.Visualization.Api;

public sealed class VisualizationHealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/visualization/health", () => Results.Ok(new { module = "Visualization", status = "ok" }));
    }
}
