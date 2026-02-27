using C4.Modules.Identity.Application.GetOrganization;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Identity.Api.Endpoints;

public sealed class GetOrganizationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/organizations/current", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrganizationQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        })
        .RequireAuthorization();
    }
}
