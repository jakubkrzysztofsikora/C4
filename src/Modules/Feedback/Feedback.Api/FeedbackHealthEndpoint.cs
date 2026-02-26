using C4.Shared.Infrastructure.Endpoints;

namespace C4.Modules.Feedback.Api;

public sealed class FeedbackHealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/feedback/health", () => Results.Ok(new { module = "Feedback", status = "ok" }));
    }
}
