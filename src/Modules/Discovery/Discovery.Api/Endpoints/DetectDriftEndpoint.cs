using C4.Modules.Discovery.Application.DetectDrift;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class DetectDriftEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/discovery/subscriptions/{subscriptionId:guid}/drift", async (Guid subscriptionId, DetectDriftRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DetectDriftCommand(subscriptionId, request.IacContent, request.Format), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record DetectDriftRequest(string IacContent, string Format);
}
