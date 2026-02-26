using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class DiscoverResourcesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/discovery/subscriptions/{subscriptionId:guid}/discover", async (Guid subscriptionId, DiscoverResourcesRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DiscoverResourcesCommand(subscriptionId, request.ExternalSubscriptionId, request.ProjectId, request.OrganizationId, request.Sources), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record DiscoverResourcesRequest(string ExternalSubscriptionId, Guid ProjectId, string? OrganizationId, IReadOnlyCollection<DiscoverySourceKind>? Sources);
}
