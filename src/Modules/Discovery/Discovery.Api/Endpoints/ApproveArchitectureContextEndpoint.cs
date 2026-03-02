using C4.Modules.Discovery.Application.ArchitectureContext;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class ApproveArchitectureContextEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/architecture-context/approve", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveArchitectureContextCommand(projectId), ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
