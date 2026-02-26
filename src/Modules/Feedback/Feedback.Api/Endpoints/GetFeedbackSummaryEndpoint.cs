using C4.Modules.Feedback.Application.GetFeedbackSummary;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Feedback.Api.Endpoints;

public sealed class GetFeedbackSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/feedback/summary", async (
            Guid projectId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetFeedbackSummaryQuery(projectId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
