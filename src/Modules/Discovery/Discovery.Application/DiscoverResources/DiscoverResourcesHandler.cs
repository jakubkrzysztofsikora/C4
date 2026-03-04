using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Application.DiscoverResources;

public sealed class DiscoverResourcesHandler(
    IDiscoveryInputPlanner planner,
    IDiscoveryInputProvider discoveryInputProvider,
    IDiscoveredResourceRepository discoveredResourceRepository,
    IResourceClassifier classifier,
    IDiscoveryDataPreparer discoveryDataPreparer,
    IMediator mediator,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork,
    ILogger<DiscoverResourcesHandler> logger,
    IProjectAuthorizationService authorizationService,
    IArchitectureContextRepository? architectureContextRepository = null,
    IArchitectureQuestionGenerator? architectureQuestionGenerator = null) : IRequestHandler<DiscoverResourcesCommand, Result<DiscoverResourcesResponse>>
{
    private const string DefaultUserIntent = "Discover Azure resources for connected subscription";

    public async Task<Result<DiscoverResourcesResponse>> Handle(DiscoverResourcesCommand request, CancellationToken cancellationToken)
    {
        Result<bool> authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (authCheck.IsFailure)
        {
            return Result<DiscoverResourcesResponse>.Failure(authCheck.Error);
        }

        try
        {
            await planner.BuildPlanAsync(
                DefaultUserIntent,
                $"SubscriptionId={request.SubscriptionId}; ExternalSubscriptionId={request.ExternalSubscriptionId}; ProjectId={request.ProjectId}",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Discovery planner failed; continuing with default pipeline");
        }

        var normalizedRequest = new NormalizedDiscoveryRequest(
            request.ProjectId,
            request.OrganizationId,
            request.ExternalSubscriptionId,
            request.Sources ?? DiscoverySourceKindDefaults.Runtime);

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
            d.ResourceId, d.ResourceType, d.Name, MapSourceProvenance(d.Source), d.ParentResourceId,
            RawPropertyReferences: d.PropertyReferences,
            ResourceGroup: d.ResourceGroup,
            Tags: d.Tags)).ToArray();
        var preparedRecords = discoveryDataPreparer.Prepare(rawRecords);

        int dataQualityFailures = 0;
        var classifiedPairs = new List<(PreparedDiscoveryRecord Record, DiscoveredResource Resource)>();
        var classificationCache = new Dictionary<string, AzureResourceClassification>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in preparedRecords)
        {
            try
            {
                if (!classificationCache.TryGetValue(record.ResourceType, out var classification))
                {
                    classification = await classifier.ClassifyAsync(request.ProjectId, record.ResourceType, record.Name, cancellationToken);
                    classificationCache[record.ResourceType] = classification;
                }
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
            .Select(p =>
            {
                IReadOnlyCollection<ResourceRelationship>? relationships = p.Record.Relationships.Count > 0
                    ? p.Record.Relationships.Select(r => new ResourceRelationship(r.RelationshipType, r.RelatedStableResourceId)).ToArray()
                    : null;
                var classification = p.Resource.Classification;
                return new DiscoveredResourceEventItem(
                    p.Resource.ResourceId,
                    p.Resource.ResourceType,
                    p.Resource.Name,
                    classification?.FriendlyName,
                    classification?.ServiceType,
                    classification?.C4Level,
                    classification?.IncludeInDiagram ?? true,
                    p.Record.RawParentResourceId,
                    p.Record.SourceProvenance,
                    p.Record.ConfidenceScore,
                    relationships,
                    p.Record.StableResourceId,
                    Domain: DeriveDomain(p.Record.Name, p.Record.ResourceGroup, p.Record.Tags),
                    IsInfrastructure: classification?.IsInfrastructure ?? false,
                    ClassificationSource: classification?.ClassificationSource ?? "fallback",
                    ClassificationConfidence: classification?.Confidence ?? 0.6,
                    ResourceGroup: p.Record.ResourceGroup,
                    Tags: p.Record.Tags);
            })
            .ToArray();

        await mediator.Publish(new ResourcesDiscoveredIntegrationEvent(request.ProjectId, diagramItems), cancellationToken);

        await PublishAppInsightsEventIfDiscoveredAsync(request.ProjectId, descriptors, cancellationToken);
        await EnsureArchitectureContextQuestionsAsync(
            request.ProjectId,
            resources.Count,
            cancellationToken);

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

    private async Task PublishAppInsightsEventIfDiscoveredAsync(
        Guid projectId,
        IReadOnlyCollection<DiscoveryResourceDescriptor> descriptors,
        CancellationToken cancellationToken)
    {
        var appInsightAppIds = descriptors
            .Where(d =>
                d.ResourceType.Equals("microsoft.insights/components", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(d.AppInsightsAppId))
            .Select(d => d.AppInsightsAppId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var appId in appInsightAppIds)
        {
            await mediator.Publish(
                new AppInsightsDiscoveredEvent(projectId, appId, string.Empty),
                cancellationToken);
        }
    }

    private static string MapSourceProvenance(DiscoverySourceKind source) => source switch
    {
        DiscoverySourceKind.AzureSubscription => "azure",
        DiscoverySourceKind.RepositoryIac => "repo",
        DiscoverySourceKind.RemoteMcp => "mcp",
        _ => source.ToString().ToLowerInvariant()
    };

    private async Task EnsureArchitectureContextQuestionsAsync(Guid projectId, int resourceCount, CancellationToken cancellationToken)
    {
        if (architectureContextRepository is null || architectureQuestionGenerator is null)
            return;

        var profile = await architectureContextRepository.GetProfileAsync(projectId, cancellationToken)
            ?? new ProjectArchitectureProfileRecord(
                projectId,
                "",
                "",
                "",
                "",
                "",
                IsApproved: false,
                LastUpdatedAtUtc: DateTime.UtcNow,
                LastQuestionGenerationAtUtc: null,
                LastResourceCount: null);

        var existingQuestions = await architectureContextRepository.GetQuestionsAsync(projectId, cancellationToken);
        bool shouldRegenerate = existingQuestions.Count == 0 || profile.LastResourceCount is null;
        if (!shouldRegenerate && profile.LastResourceCount is int previousCount && previousCount > 0)
        {
            var delta = Math.Abs(resourceCount - previousCount) / (double)previousCount;
            shouldRegenerate = delta >= 0.15;
        }

        if (!shouldRegenerate)
            return;

        string summary = $"""
            Project description: {profile.ProjectDescription}
            System boundaries: {profile.SystemBoundaries}
            Core domains: {profile.CoreDomains}
            External dependencies: {profile.ExternalDependencies}
            Data sensitivity: {profile.DataSensitivity}
            Resource count: {resourceCount}
            """;

        var generated = await architectureQuestionGenerator.GenerateQuestionsAsync(projectId, summary, cancellationToken);
        var questions = generated
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(q => new ProjectArchitectureQuestionRecord(
                Guid.NewGuid(),
                projectId,
                q.Trim(),
                Answer: null,
                IsApproved: false,
                CreatedAtUtc: DateTime.UtcNow,
                AnsweredAtUtc: null))
            .ToArray();

        await architectureContextRepository.ReplaceQuestionsAsync(projectId, questions, cancellationToken);
        await architectureContextRepository.UpsertProfileAsync(profile with
        {
            IsApproved = false,
            LastUpdatedAtUtc = DateTime.UtcNow,
            LastQuestionGenerationAtUtc = DateTime.UtcNow,
            LastResourceCount = resourceCount
        }, cancellationToken);
    }

    internal static string DeriveDomain(string name, string? resourceGroup, IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is not null)
        {
            if (TryGetTag(tags, "Service", out var service) || TryGetTag(tags, "service", out service))
                return NormalizeDomain(service);

            if (TryGetTag(tags, "Application", out var application) || TryGetTag(tags, "application", out application))
                return NormalizeDomain(application);

            if (TryGetTag(tags, "ManagedBy", out var managedBy))
                return NormalizeDomain(managedBy);
        }

        var combined = $"{resourceGroup} {name}".ToLowerInvariant();
        if (combined.Contains("document-service") || combined.Contains("documents"))
            return "DocumentService";
        if (combined.Contains("-ob") || combined.Contains("openbank") || combined.Contains("banking"))
            return "OpenBanking";
        if (combined.Contains("circit-") || combined.Contains("coreapp") || combined.Contains("app-circit"))
            return "CoreApp";
        if (combined.Contains("grafana") || combined.Contains("monitoring") || combined.Contains("mcp"))
            return "Platform";
        return "General";
    }

    private static bool TryGetTag(IReadOnlyDictionary<string, string> tags, string key, out string value)
    {
        if (tags.TryGetValue(key, out var candidate) && !string.IsNullOrWhiteSpace(candidate))
        {
            value = candidate;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string NormalizeDomain(string input)
    {
        var cleaned = input.Trim();
        if (cleaned.Length == 0)
            return "General";

        if (cleaned.Equals("coreapp", StringComparison.OrdinalIgnoreCase))
            return "CoreApp";
        if (cleaned.Equals("documentservice", StringComparison.OrdinalIgnoreCase))
            return "DocumentService";

        return cleaned.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
