using C4.Shared.Infrastructure.Endpoints;

namespace C4.Modules.Identity.Api;

public sealed class IdentityHealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/identity/health", () => Results.Ok(new { module = "Identity", status = "ok" }));
    }
}
