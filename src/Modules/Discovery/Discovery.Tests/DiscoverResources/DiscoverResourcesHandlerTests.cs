using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
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
        var handler = new DiscoverResourcesHandler(new FakeDiscoveryInputPlanner(), new FakeDiscoveryInputProvider(), repo, classifier, new FakeMediator(), new FakeUnitOfWork());
        var subscriptionId = Guid.NewGuid();

        var result = await handler.Handle(new DiscoverResourcesCommand(subscriptionId, "sub-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourcesCount.Should().Be(2);
        (await repo.GetBySubscriptionAsync(subscriptionId, CancellationToken.None)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ResourcesWithIncludeInDiagramFalse_ExcludedFromIntegrationEvent()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var classifier = new FakeResourceClassifier();
        var mediator = new CapturingMediator();
        var handler = new DiscoverResourcesHandler(new FakeDiscoveryInputPlanner(), new MixedDiscoveryInputProvider(), repo, classifier, mediator, new FakeUnitOfWork());
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
        var handler = new DiscoverResourcesHandler(new FakeDiscoveryInputPlanner(), new FakeDiscoveryInputProvider(), repo, classifier, mediator, new FakeUnitOfWork());

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
        var handler = new DiscoverResourcesHandler(new FakeDiscoveryInputPlanner(), new FakeDiscoveryInputProvider(), repo, classifier, mediator, new FakeUnitOfWork());

        await handler.Handle(new DiscoverResourcesCommand(Guid.NewGuid(), "sub-1", Guid.NewGuid()), CancellationToken.None);

        mediator.PublishedEvent.Should().NotBeNull();
        var childItem = mediator.PublishedEvent!.Resources.Single(r => r.ResourceId == "/r2");
        childItem.ParentResourceId.Should().Be("/r1");
    }

    [Fact]
    public async Task Handle_PlannerInvokedBeforeDiscovery_SetsPlanOnResponse()
    {
        var state = new PlannerState();
        var planner = new FakeDiscoveryInputPlanner(state);
        var handler = new DiscoverResourcesHandler(planner, new OrderAwareDiscoveryInputProvider(state), new FakeDiscoveredResourceRepository(), new FakeResourceClassifier(), new FakeMediator(), new FakeUnitOfWork());

        var result = await handler.Handle(new DiscoverResourcesCommand(Guid.NewGuid(), "sub-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Plan.Tasks.Should().NotBeEmpty();
        state.PlannerCalled.Should().BeTrue();
    }

    private sealed class FakeDiscoveryInputProvider : IDiscoveryInputProvider
    {
        public Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DiscoveryResourceDescriptor>>(
            [
                new("/r1", "Microsoft.Web/sites", "frontend", null, DiscoverySourceKind.AzureSubscription),
                new("/r2", "Microsoft.Web/sites", "api", "/r1", DiscoverySourceKind.AzureSubscription),
            ]);
    }

    private sealed class MixedDiscoveryInputProvider : IDiscoveryInputProvider
    {
        public Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DiscoveryResourceDescriptor>>(
            [
                new("/r1", "Microsoft.Web/sites", "frontend", null, DiscoverySourceKind.AzureSubscription),
                new("/r2", "Microsoft.Network/networkSecurityGroups", "nsg", null, DiscoverySourceKind.AzureSubscription),
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

    private sealed class FakeDiscoveryInputPlanner(PlannerState? state = null) : IDiscoveryInputPlanner
    {
        public Task<DiscoveryPlan> BuildPlanAsync(string userIntent, string inputContext, CancellationToken cancellationToken)
        {
            if (state is not null)
                state.PlannerCalled = true;

            return Task.FromResult(new DiscoveryPlan(
                userIntent,
                inputContext,
                [new PlannedToolInvocation("t1", 1, "azure.resource_graph", "Discover resources", [], ["repo.bicep_parser"])]));
        }
    }

    private sealed class PlannerState
    {
        public bool PlannerCalled { get; set; }
    }

    private sealed class OrderAwareDiscoveryInputProvider(PlannerState state) : IDiscoveryInputProvider
    {
        public Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(NormalizedDiscoveryRequest request, CancellationToken cancellationToken)
        {
            state.PlannerCalled.Should().BeTrue("planner must run before resource discovery");
            return Task.FromResult<IReadOnlyCollection<DiscoveryResourceDescriptor>>(
            [
                new("/r1", "Microsoft.Web/sites", "frontend", null, DiscoverySourceKind.AzureSubscription),
            ]);
        }
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
