using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DetectDrift;

public sealed class DetectDriftHandler(
    IIacStateParser parser,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IDriftResultRepository driftResultRepository,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<DetectDriftCommand, Result<DetectDriftResponse>>
{
    public async Task<Result<DetectDriftResponse>> Handle(DetectDriftCommand request, CancellationToken cancellationToken)
    {
        var desired = await parser.ParseAsync(request.IacContent, request.Format, cancellationToken);
        var actual = await discoveredResourceRepository.GetBySubscriptionAsync(request.SubscriptionId, cancellationToken);

        var desiredSet = desired.Select(d => d.ResourceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var driftItems = actual
            .Select(resource => new DriftItem(resource.ResourceId, desiredSet.Contains(resource.ResourceId) ? "InSync" : "Drifted"))
            .ToArray();

        await driftResultRepository.SaveAsync(request.SubscriptionId, driftItems, cancellationToken);

        await mediator.Publish(new DriftDetectedIntegrationEvent(
            request.SubscriptionId,
            driftItems.Select(item => new DriftDetectedEventItem(item.ResourceId, item.Status)).ToArray()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var drifted = driftItems.Count(item => item.Status == "Drifted");
        return Result<DetectDriftResponse>.Success(new DetectDriftResponse(request.SubscriptionId, drifted, driftItems.Length - drifted));
    }
}
