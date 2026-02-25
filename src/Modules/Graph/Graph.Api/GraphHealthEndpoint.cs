using C4.Shared.Infrastructure.Endpoints;

namespace C4.Modules.Graph.Api;

public sealed class GraphHealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/graph/health", () => Results.Ok(new { module = "Graph", status = "ok" }));
    }
}
