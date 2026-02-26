using System.Security.Claims;
using C4.Modules.Feedback.Application.SubmitFeedback;
using C4.Modules.Feedback.Domain.Corrections;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Feedback.Api.Endpoints;

public sealed class SubmitFeedbackEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/feedback", async (
            Guid projectId,
            SubmitFeedbackRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new SubmitFeedbackCommand(
                projectId,
                request.TargetType,
                request.TargetId,
                request.Category,
                request.Rating,
                request.Comment,
                request.NodeCorrection,
                request.EdgeCorrection,
                request.ClassificationCorrection,
                userId);

            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{projectId}/feedback/{result.Value.FeedbackEntryId}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record SubmitFeedbackRequest(
        FeedbackTargetType TargetType,
        Guid TargetId,
        FeedbackCategory Category,
        int Rating,
        string? Comment,
        NodeCorrection? NodeCorrection,
        EdgeCorrection? EdgeCorrection,
        ClassificationCorrection? ClassificationCorrection);
}
