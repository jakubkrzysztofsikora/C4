using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoverResourcesHandler(
    IDiscoveryInputPlanner planner,
    IDiscoveryInputProvider discoveryInputProvider,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IResourceClassifier classifier,
    IDiscoveryDataPreparer discoveryDataPreparer,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<DiscoverResourcesCommand, Result<DiscoverResourcesResponse>>
{
    private const string DefaultUserIntent = "Discover Azure resources for connected subscription";

    public async Task<Result<DiscoverResourcesResponse>> Handle(DiscoverResourcesCommand request, CancellationToken cancellationToken)
    {
        await planner.BuildPlanAsync(
            DefaultUserIntent,
            $"SubscriptionId={request.SubscriptionId}; ExternalSubscriptionId={request.ExternalSubscriptionId}; ProjectId={request.ProjectId}",
            cancellationToken);

        var normalizedRequest = new NormalizedDiscoveryRequest(
            request.ProjectId,
            request.OrganizationId,
            request.ExternalSubscriptionId,
            request.Sources ?? DiscoverySourceKindDefaults.All);

        IReadOnlyCollection<DiscoveryResourceDescriptor> descriptors;
        try
        {
            descriptors = await discoveryInputProvider.GetResourcesAsync(normalizedRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<DiscoverResourcesResponse>.Failure(DiscoveryEscalationMapper.MapExternalFailure(ex));
        }

        var rawRecords = descriptors.Select(d => new RawDiscoveryRecord(
            d.ResourceId, d.ResourceType, d.Name, MapSourceProvenance(d.Source), d.ParentResourceId)).ToArray();
        var preparedRecords = discoveryDataPreparer.Prepare(rawRecords);

        int dataQualityFailures = 0;
        var classifiedPairs = new List<(PreparedDiscoveryRecord Record, DiscoveredResource Resource)>();
        foreach (var record in preparedRecords)
        {
            try
            {
                var classification = await classifier.ClassifyAsync(request.ProjectId, record.ResourceType, record.Name, cancellationToken);
                var resource = DiscoveredResource.Create(record.RawResourceId ?? record.StableResourceId, record.ResourceType, record.Name, classification);
                classifiedPairs.Add((record, resource));
            }
            catch
            {
                dataQualityFailures++;
            }
        }

        var resources = classifiedPairs.Select(p => p.Resource).ToList();
        await discoveredResourceRepository.UpsertRangeAsync(request.SubscriptionId, resources, cancellationToken);

        var diagramItems = classifiedPairs
            .Where(p => p.Resource.Classification?.IncludeInDiagram ?? true)
            .Select(p => new DiscoveredResourceEventItem(
                p.Resource.ResourceId,
                p.Resource.ResourceType,
                p.Resource.Name,
                p.Resource.Classification?.FriendlyName,
                p.Resource.Classification?.ServiceType,
                p.Resource.Classification?.C4Level,
                p.Resource.Classification?.IncludeInDiagram ?? true,
                p.Record.RawParentResourceId,
                p.Record.SourceProvenance,
                p.Record.ConfidenceScore,
                p.Record.Relationships.FirstOrDefault()?.RelationshipType,
                p.Record.Relationships.FirstOrDefault()?.RelatedStableResourceId,
                p.Record.StableResourceId))
            .ToArray();

        await mediator.Publish(new ResourcesDiscoveredIntegrationEvent(request.ProjectId, diagramItems), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var escalation = dataQualityFailures > 0
            ? DiscoveryEscalationMapper.ForPartialDataQuality()
            : DiscoveryEscalationMapper.ForSuccess();

        return Result<DiscoverResourcesResponse>.Success(
            new DiscoverResourcesResponse(
                request.SubscriptionId,
                resources.Count,
                escalation.Status,
                escalation.EscalationLevel,
                escalation.UserActionHint,
                dataQualityFailures));
    }

    private static string MapSourceProvenance(DiscoverySourceKind source) => source switch
    {
        DiscoverySourceKind.AzureSubscription => "azure",
        DiscoverySourceKind.RepositoryIac => "repo",
        DiscoverySourceKind.RemoteMcp => "mcp",
        _ => source.ToString().ToLowerInvariant()
    };
}
