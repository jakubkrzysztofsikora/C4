using C4.Modules.Discovery.Application.GetDiscoveryStatus;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Resources;

namespace C4.Modules.Discovery.Tests.GetDiscoveryStatus;

public sealed class GetDiscoveryStatusHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAggregatedStatus()
    {
        var handler = new GetDiscoveryStatusHandler(new FakeDiscoveredResourceRepository(), new FakeDriftRepository());

        var result = await handler.Handle(new GetDiscoveryStatusQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DiscoveredResources.Should().Be(2);
        result.Value.DriftedResources.Should().Be(1);
    }

    private sealed class FakeDiscoveredResourceRepository : IDiscoveredResourceRepository
    {
        public Task UpsertRangeAsync(Guid subscriptionId, IReadOnlyCollection<DiscoveredResource> resources, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyCollection<DiscoveredResource>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DiscoveredResource>>([
                DiscoveredResource.Create("/r1", "t", "one"),
                DiscoveredResource.Create("/r2", "t", "two")
            ]);
    }

    private sealed class FakeDriftRepository : IDriftResultRepository
    {
        public Task SaveAsync(Guid subscriptionId, IReadOnlyCollection<DriftItem> driftItems, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyCollection<DriftItem>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DriftItem>>([
                new DriftItem("/r1", "Drifted"),
                new DriftItem("/r2", "InSync")
            ]);
    }
}
