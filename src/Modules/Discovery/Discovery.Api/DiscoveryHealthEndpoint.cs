using C4.Shared.Infrastructure.Endpoints;

namespace C4.Modules.Discovery.Api;

public sealed class DiscoveryHealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/discovery/health", () => Results.Ok(new { module = "Discovery", status = "ok" }));
    }
}
