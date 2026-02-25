using C4.Modules.Visualization.Application.GetViewPresets;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Visualization.Api.Endpoints;

public sealed class GetViewPresetsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/view-presets", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetViewPresetsQuery(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}
