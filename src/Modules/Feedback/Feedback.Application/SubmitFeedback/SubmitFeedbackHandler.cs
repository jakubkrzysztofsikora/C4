using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Feedback.Application.SubmitFeedback;

public sealed class SubmitFeedbackHandler(
    IFeedbackEntryRepository repository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<SubmitFeedbackHandler> logger)
    : IRequestHandler<SubmitFeedbackCommand, Result<SubmitFeedbackResponse>>
{
    public async Task<Result<SubmitFeedbackResponse>> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var ratingResult = FeedbackRating.Create(request.Rating);
        if (ratingResult.IsFailure)
        {
            return Result<SubmitFeedbackResponse>.Failure(ratingResult.Error);
        }

        var target = new FeedbackTarget(request.TargetType, request.TargetId);

        var hasCorrection = request.NodeCorrection is not null
            || request.EdgeCorrection is not null
            || request.ClassificationCorrection is not null;

        var entry = FeedbackEntry.Submit(
            request.UserId,
            request.ProjectId,
            target,
            request.Category,
            ratingResult.Value,
            request.Comment,
            request.NodeCorrection,
            request.EdgeCorrection,
            request.ClassificationCorrection);

        await repository.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FeedbackSubmitted: ProjectId={ProjectId}, Category={Category}, Rating={Rating}, HasCorrection={HasCorrection}",
            request.ProjectId, request.Category, request.Rating, hasCorrection);

        await mediator.Publish(
            new FeedbackSubmittedIntegrationEvent(request.ProjectId, entry.Id.Value, request.Category.ToString(), request.Rating),
            cancellationToken);

        return Result<SubmitFeedbackResponse>.Success(new SubmitFeedbackResponse(entry.Id.Value));
    }
}
