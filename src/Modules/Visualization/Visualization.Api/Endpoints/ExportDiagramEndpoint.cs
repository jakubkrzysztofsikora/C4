using C4.Modules.Visualization.Application.ExportDiagram;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Visualization.Api.Endpoints;

public sealed class ExportDiagramEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/diagram/export", async (Guid projectId, string format, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ExportDiagramCommand(projectId, format), ct);
            return result.IsSuccess ? Results.File(result.Value.Content, result.Value.ContentType) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();

        app.MapPost("/api/projects/{projectId:guid}/diagram/export", async (
            Guid projectId,
            string format,
            ExportDiagramRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ExportDiagramCommand(projectId, format, request.DiagramJson), ct);
            return result.IsSuccess ? Results.File(result.Value.Content, result.Value.ContentType) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record ExportDiagramRequest(string? DiagramJson);
}
