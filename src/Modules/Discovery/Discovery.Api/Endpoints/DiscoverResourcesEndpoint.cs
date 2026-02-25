using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class DiscoverResourcesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/discovery/subscriptions/{subscriptionId:guid}/discover", async (Guid subscriptionId, DiscoverResourcesRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DiscoverResourcesCommand(subscriptionId, request.ExternalSubscriptionId, request.ProjectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }

    public sealed record DiscoverResourcesRequest(string ExternalSubscriptionId, Guid ProjectId);
}
