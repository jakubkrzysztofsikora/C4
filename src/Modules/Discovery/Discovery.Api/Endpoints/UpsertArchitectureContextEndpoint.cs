using C4.Modules.Discovery.Application.ArchitectureContext;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class UpsertArchitectureContextEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/projects/{projectId:guid}/architecture-context", async (Guid projectId, UpsertArchitectureContextRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpsertArchitectureContextCommand(
                    projectId,
                    request.ProjectDescription,
                    request.SystemBoundaries,
                    request.CoreDomains,
                    request.ExternalDependencies,
                    request.DataSensitivity),
                ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record UpsertArchitectureContextRequest(
        string ProjectDescription,
        string SystemBoundaries,
        string CoreDomains,
        string ExternalDependencies,
        string DataSensitivity);
}
