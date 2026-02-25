using C4.Modules.Discovery.Application.ConnectAzureSubscription;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class ConnectAzureSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/discovery/subscriptions", async (ConnectAzureSubscriptionRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ConnectAzureSubscriptionCommand(request.ExternalSubscriptionId, request.DisplayName), cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/discovery/subscriptions/{result.Value.SubscriptionId}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record ConnectAzureSubscriptionRequest(string ExternalSubscriptionId, string DisplayName);
}
