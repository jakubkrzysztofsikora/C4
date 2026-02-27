using C4.Modules.Discovery.Application.GetAzureAuthUrl;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class GetAzureAuthUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/azure/auth", async (string redirectUri, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAzureAuthUrlQuery(redirectUri), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
