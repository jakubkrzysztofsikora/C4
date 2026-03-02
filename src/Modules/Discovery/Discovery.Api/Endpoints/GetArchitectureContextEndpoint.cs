using C4.Modules.Discovery.Application.ArchitectureContext;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class GetArchitectureContextEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/architecture-context", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetArchitectureContextQuery(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
