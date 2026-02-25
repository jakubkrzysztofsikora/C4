using C4.Modules.Graph.Application.AnalyzeArchitecture;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class AnalyzeArchitectureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/analyze", async (Guid projectId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AnalyzeArchitectureCommand(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .RequireAuthorization();
    }
}
