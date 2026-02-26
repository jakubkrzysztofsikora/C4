using C4.Modules.Feedback.Application.Ports;
using C4.Shared.Kernel;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Feedback.Application.AggregateLearnings;

public sealed class AggregateLearningsHandler(
    IFeedbackEntryRepository feedbackRepository,
    ILearningAggregator aggregator,
    ILearningInsightRepository insightRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<AggregateLearningsHandler> logger)
    : IRequestHandler<AggregateLearningsCommand, Result<AggregateLearningsResponse>>
{
    public async Task<Result<AggregateLearningsResponse>> Handle(AggregateLearningsCommand request, CancellationToken cancellationToken)
    {
        var entries = await feedbackRepository.GetByProjectForAggregationAsync(request.ProjectId, request.Category, cancellationToken);

        if (entries.Count == 0)
        {
            return Result<AggregateLearningsResponse>.Success(new AggregateLearningsResponse(0));
        }

        var insights = await aggregator.AggregateAsync(request.ProjectId, entries, cancellationToken);

        foreach (var insight in insights)
        {
            await insightRepository.AddAsync(insight, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "LearningsAggregated: ProjectId={ProjectId}, FeedbackCount={FeedbackCount}, InsightsGenerated={InsightsGenerated}",
            request.ProjectId, entries.Count, insights.Count);

        await mediator.Publish(
            new LearningsUpdatedIntegrationEvent(request.ProjectId, insights.Count),
            cancellationToken);

        return Result<AggregateLearningsResponse>.Success(new AggregateLearningsResponse(insights.Count));
    }
}
