using C4.Modules.Feedback.Application.AggregateLearnings;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Feedback.Api.Endpoints;

public sealed class AggregateLearningsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/feedback/aggregate", async (
            Guid projectId,
            ISender sender,
            FeedbackCategory? category = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new AggregateLearningsCommand(projectId, category), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }
}
