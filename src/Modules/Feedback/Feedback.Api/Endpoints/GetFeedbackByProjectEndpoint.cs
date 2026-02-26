using C4.Modules.Feedback.Application.GetFeedbackByProject;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Feedback.Api.Endpoints;

public sealed class GetFeedbackByProjectEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/feedback", async (
            Guid projectId,
            ISender sender,
            int skip = 0,
            int take = 20,
            FeedbackCategory? category = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetFeedbackByProjectQuery(projectId, skip, take, category), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
