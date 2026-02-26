using C4.Modules.Feedback.Application.GetEvalMetrics;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Feedback.Api.Endpoints;

public sealed class GetEvalMetricsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/feedback/eval-metrics", async (
            Guid projectId,
            ISender sender,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetEvalMetricsQuery(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
