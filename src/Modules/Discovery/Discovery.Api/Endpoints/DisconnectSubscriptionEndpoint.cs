using C4.Modules.Discovery.Application.DisconnectSubscription;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class DisconnectSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/discovery/subscriptions/current", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DisconnectSubscriptionCommand(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
