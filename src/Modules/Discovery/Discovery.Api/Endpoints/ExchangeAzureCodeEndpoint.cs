using C4.Modules.Discovery.Application.ExchangeAzureCode;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class ExchangeAzureCodeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/azure/auth/callback", async (ExchangeAzureCodeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ExchangeAzureCodeCommand(request.Code, request.RedirectUri, request.State), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record ExchangeAzureCodeRequest(string Code, string RedirectUri, string State);
}
