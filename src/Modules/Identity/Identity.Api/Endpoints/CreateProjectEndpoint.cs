using C4.Modules.Identity.Application.CreateProject;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Identity.Api.Endpoints;

public sealed class CreateProjectEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/organizations/{organizationId:guid}/projects", async (Guid organizationId, CreateProjectRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateProjectCommand(organizationId, request.Name), cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/organizations/{organizationId}/projects/{result.Value.ProjectId}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record CreateProjectRequest(string Name);
}
