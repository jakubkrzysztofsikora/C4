using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Errors;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;

namespace C4.Modules.Discovery.Tests.DiscoverResources;

public sealed class DiscoverResourcesHandlerTests
{
    [Fact]
    public async Task Handle_ValidSubscription_PersistsResources()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var classifier = new FakeResourceClassifier();
        var handler = new DiscoverResourcesHandler(new FakeResourceGraphClient(), repo, classifier, new FakeMediator(), new FakeUnitOfWork());
        var subscriptionId = Guid.NewGuid();

        var result = await handler.Handle(new DiscoverResourcesCommand(subscriptionId, "sub-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourcesCount.Should().Be(2);
        result.Value.Status.Should().Be(DiscoveryExecutionStatus.Success);
        result.Value.EscalationLevel.Should().Be(DiscoveryEscalationLevel.RetrySilently);
        (await repo.GetBySubscriptionAsync(subscriptionId, CancellationToken.None)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ClassificationFailure_ReturnsPartialWithNotifyEscalation()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var classifier = new FaultyResourceClassifier();
        var handler = new DiscoverResourcesHandler(new FakeResourceGraphClient(), repo, classifier, new FakeMediator(), new FakeUnitOfWork());

        var result = await handler.Handle(new DiscoverResourcesCommand(Guid.NewGuid(), "sub-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DiscoveryExecutionStatus.Partial);
        result.Value.EscalationLevel.Should().Be(DiscoveryEscalationLevel.NotifyUser);
        result.Value.DataQualityFailures.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ConnectorUnavailable_ReturnsFailureWithRetryableError()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var classifier = new FakeResourceClassifier();
        var handler = new DiscoverResourcesHandler(new UnavailableResourceGraphClient(), repo, classifier, new FakeMediator(), new FakeUnitOfWork());

        var result = await handler.Handle(new DiscoverResourcesCommand(Guid.NewGuid(), "sub-1", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DiscoveryErrors.ConnectorUnavailable("azure-resource-graph"));
    }

    [Fact]
    public async Task Handle_ResourcesWithIncludeInDiagramFalse_ExcludedFromIntegrationEvent()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var classifier = new FakeResourceClassifier();
        var mediator = new CapturingMediator();
        var handler = new DiscoverResourcesHandler(new MixedResourceGraphClient(), repo, classifier, mediator, new FakeUnitOfWork());
        var subscriptionId = Guid.NewGuid();

        await handler.Handle(new DiscoverResourcesCommand(subscriptionId, "sub-1", Guid.NewGuid()), CancellationToken.None);

        mediator.PublishedEvent.Should().NotBeNull();
        mediator.PublishedEvent!.Resources.Should().OnlyContain(r => r.IncludeInDiagram);
    }

    [Fact]
    public async Task Handle_ClassificationEnrichedOnResource_FriendlyNamePropagatedToEvent()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var classifier = new FakeResourceClassifier();
        var mediator = new CapturingMediator();
        var handler = new DiscoverResourcesHandler(new FakeResourceGraphClient(), repo, classifier, mediator, new FakeUnitOfWork());

        await handler.Handle(new DiscoverResourcesCommand(Guid.NewGuid(), "sub-1", Guid.NewGuid()), CancellationToken.None);

        mediator.PublishedEvent.Should().NotBeNull();
        mediator.PublishedEvent!.Resources.Should().AllSatisfy(r => r.FriendlyName.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task Handle_ResourceWithParent_ParentIdInEvent()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var classifier = new FakeResourceClassifier();
        var mediator = new CapturingMediator();
        var handler = new DiscoverResourcesHandler(new FakeResourceGraphClient(), repo, classifier, mediator, new FakeUnitOfWork());

        await handler.Handle(new DiscoverResourcesCommand(Guid.NewGuid(), "sub-1", Guid.NewGuid()), CancellationToken.None);

        mediator.PublishedEvent.Should().NotBeNull();
        var childItem = mediator.PublishedEvent!.Resources.Single(r => r.ResourceId == "/r2");
        childItem.ParentResourceId.Should().Be("/r1");
    }

    private sealed class FakeResourceGraphClient : IAzureResourceGraphClient
    {
        public Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<AzureResourceRecord>>(
            [new("/r1", "Microsoft.Web/sites", "frontend", null), new("/r2", "Microsoft.Web/sites", "api", "/r1")]);
    }

    private sealed class MixedResourceGraphClient : IAzureResourceGraphClient
    {
        public Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<AzureResourceRecord>>(
            [
                new("/r1", "Microsoft.Web/sites", "frontend", null),
                new("/r2", "Microsoft.Network/networkSecurityGroups", "nsg", null),
            ]);
    }

    private sealed class FakeResourceClassifier : IResourceClassifier
    {
        public Task<AzureResourceClassification> ClassifyAsync(string armResourceType, string resourceName, CancellationToken cancellationToken)
        {
            var classification = AzureResourceTypeCatalog.Classify(armResourceType);
            return Task.FromResult(classification);
        }
    }

    private sealed class FaultyResourceClassifier : IResourceClassifier
    {
        public Task<AzureResourceClassification> ClassifyAsync(string armResourceType, string resourceName, CancellationToken cancellationToken)
        {
            if (resourceName == "api")
            {
                throw new InvalidOperationException("Bad resource payload");
            }

            return Task.FromResult(AzureResourceTypeCatalog.Classify(armResourceType));
        }
    }

    private sealed class UnavailableResourceGraphClient : IAzureResourceGraphClient
    {
        public Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
            => throw new HttpRequestException("Connector unavailable");
    }

    private sealed class FakeDiscoveredResourceRepository : IDiscoveredResourceRepository
    {
        private readonly Dictionary<Guid, IReadOnlyCollection<DiscoveredResource>> _store = [];

        public Task UpsertRangeAsync(Guid subscriptionId, IReadOnlyCollection<DiscoveredResource> resources, CancellationToken cancellationToken)
        {
            _store[subscriptionId] = resources;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<DiscoveredResource>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
            => Task.FromResult(_store.TryGetValue(subscriptionId, out var value) ? value : (IReadOnlyCollection<DiscoveredResource>)[]);
    }

    private sealed class CapturingMediator : IMediator
    {
        public ResourcesDiscoveredIntegrationEvent? PublishedEvent { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            if (notification is ResourcesDiscoveredIntegrationEvent evt)
                PublishedEvent = evt;
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> => throw new NotImplementedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotImplementedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeMediator : IMediator
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> => throw new NotImplementedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotImplementedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
