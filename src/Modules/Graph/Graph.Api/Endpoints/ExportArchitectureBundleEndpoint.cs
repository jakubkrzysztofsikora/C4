using C4.Modules.Graph.Application.ExportArchitectureBundle;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class ExportArchitectureBundleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/architecture/bundle", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ExportArchitectureBundleCommand(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .RequireAuthorization();
    }
}
