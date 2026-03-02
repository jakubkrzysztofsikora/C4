using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.DetectDrift;

public sealed class DetectDriftHandler(
    IIacStateParser parser,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IDriftResultRepository driftResultRepository,
    IMediator mediator,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork) : IRequestHandler<DetectDriftCommand, Result<DetectDriftResponse>>
{
    public async Task<Result<DetectDriftResponse>> Handle(DetectDriftCommand request, CancellationToken cancellationToken)
    {
        var desired = await parser.ParseAsync(request.IacContent, request.Format, cancellationToken);
        var actual = await discoveredResourceRepository.GetBySubscriptionAsync(request.SubscriptionId, cancellationToken);

        var desiredSet = desired.Select(d => d.ResourceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> desiredNameHints = desired
            .Select(d => ExtractTerminalResourceName(d.ResourceId))
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var driftItems = actual
            .Select(resource => new DriftItem(
                resource.ResourceId,
                IsInSync(resource.ResourceId, desiredSet, desiredNameHints) ? "InSync" : "Drifted"))
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

    private static bool IsInSync(
        string actualResourceId,
        IReadOnlySet<string> desiredSet,
        IReadOnlySet<string> desiredNameHints)
    {
        if (desiredSet.Contains(actualResourceId))
            return true;

        var actualName = ExtractTerminalResourceName(actualResourceId);
        if (actualName is null)
            return false;

        return desiredNameHints.Contains(actualName);
    }

    private static string? ExtractTerminalResourceName(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId)) return null;
        var segments = resourceId
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0) return null;
        return segments[^1];
    }
}
