using C4.Modules.Discovery.Application.GetDiscoveryStatus;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class GetDiscoveryStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/discovery/subscriptions/{subscriptionId:guid}/status", async (Guid subscriptionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDiscoveryStatusQuery(subscriptionId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
