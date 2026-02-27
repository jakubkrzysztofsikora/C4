using C4.Modules.Discovery.Application.GetSubscription;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class GetSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/discovery/subscriptions/current", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSubscriptionQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        })
        .RequireAuthorization();
    }
}
