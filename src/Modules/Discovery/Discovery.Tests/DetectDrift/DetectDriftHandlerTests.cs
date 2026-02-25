using C4.Modules.Discovery.Application.DetectDrift;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Tests.DetectDrift;

public sealed class DetectDriftHandlerTests
{
    [Fact]
    public async Task Handle_MissingResource_ReportsDrift()
    {
        var subId = Guid.NewGuid();
        var handler = new DetectDriftHandler(new FakeParser(), new FakeDiscoveredResourceRepo(), new FakeDriftRepo(), new FakeMediator(), new FakeUow());

        var result = await handler.Handle(new DetectDriftCommand(subId, "resource x", "bicep"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DriftedCount.Should().Be(1);
    }

    private sealed class FakeParser : IIacStateParser
    {
        public Task<IReadOnlyCollection<IacResourceRecord>> ParseAsync(string iacContent, string format, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<IacResourceRecord>>([new("/r1", "t", "n")]);
    }

    private sealed class FakeDiscoveredResourceRepo : IDiscoveredResourceRepository
    {
        public Task UpsertRangeAsync(Guid subscriptionId, IReadOnlyCollection<DiscoveredResource> resources, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyCollection<DiscoveredResource>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DiscoveredResource>>([DiscoveredResource.Create("/r2", "t", "n2")]);
    }

    private sealed class FakeDriftRepo : IDriftResultRepository
    {
        public Task SaveAsync(Guid subscriptionId, IReadOnlyCollection<DriftItem> driftItems, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyCollection<DriftItem>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DriftItem>>([]);
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

    private sealed class FakeUow : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
