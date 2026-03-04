using C4.Modules.Graph.Application.GetDriftOverview;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

public sealed class GetDriftOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        static async Task<IResult> HandleGetDrift(Guid projectId, ISender sender, CancellationToken ct)
        {
            var result = await sender.Send(new GetDriftOverviewQuery(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }

        app.MapGet("/api/projects/{projectId:guid}/drift", HandleGetDrift)
            .RequireAuthorization();
        app.MapGet("/api/projects/{projectId:guid}/drift/runs/latest", HandleGetDrift)
            .RequireAuthorization();
    }
}
