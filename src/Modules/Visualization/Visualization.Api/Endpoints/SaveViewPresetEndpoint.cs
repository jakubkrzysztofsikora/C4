using C4.Modules.Visualization.Application.SaveViewPreset;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Visualization.Api.Endpoints;

public sealed class SaveViewPresetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/view-presets", async (Guid projectId, SaveViewPresetRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SaveViewPresetCommand(projectId, request.Name, request.Json), ct);
            return result.IsSuccess ? Results.Created($"/api/projects/{projectId}/view-presets/{result.Value.PresetId}", result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record SaveViewPresetRequest(string Name, string Json);
}
