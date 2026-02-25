using C4.Modules.Identity.Application.RegisterOrganization;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Identity.Api.Endpoints;

public sealed class RegisterOrganizationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/organizations", async (RegisterOrganizationRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RegisterOrganizationCommand(request.Name), cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/organizations/{result.Value.OrganizationId}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record RegisterOrganizationRequest(string Name);
}
