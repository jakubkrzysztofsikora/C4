using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Tests.DiscoverResources;

public sealed class DiscoverResourcesHandlerTests
{
    [Fact]
    public async Task Handle_ValidSubscription_PersistsResources()
    {
        var repo = new FakeDiscoveredResourceRepository();
        var handler = new DiscoverResourcesHandler(new FakeResourceGraphClient(), repo, new FakeMediator(), new FakeUnitOfWork());
        var subscriptionId = Guid.NewGuid();

        var result = await handler.Handle(new DiscoverResourcesCommand(subscriptionId, "sub-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourcesCount.Should().Be(2);
        (await repo.GetBySubscriptionAsync(subscriptionId, CancellationToken.None)).Should().HaveCount(2);
    }

    private sealed class FakeResourceGraphClient : IAzureResourceGraphClient
    {
        public Task<IReadOnlyCollection<AzureResourceRecord>> GetResourcesAsync(string externalSubscriptionId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<AzureResourceRecord>>(
            [new("/r1", "Microsoft.Web/sites", "frontend", null), new("/r2", "Microsoft.Web/sites", "api", "/r1")]);
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
