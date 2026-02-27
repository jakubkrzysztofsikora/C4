using C4.Modules.Discovery.Application.ConnectAzureSubscription;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Subscriptions;
using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Tests;

public sealed class ConnectAzureSubscriptionHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ConnectsSubscription()
    {
        var repository = new FakeAzureSubscriptionRepository();
        var handler = new ConnectAzureSubscriptionHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(new ConnectAzureSubscriptionCommand("sub-001", "Production"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalSubscriptionId.Should().Be("sub-001");
    }

    [Fact]
    public async Task Handle_DuplicateSubscription_ReturnsError()
    {
        var repository = new FakeAzureSubscriptionRepository();
        await repository.AddAsync(AzureSubscription.Connect("sub-001", "Production").Value, CancellationToken.None);
        var handler = new ConnectAzureSubscriptionHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(new ConnectAzureSubscriptionCommand("sub-001", "Prod"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("discovery.subscription.duplicate");
    }

    private sealed class FakeAzureSubscriptionRepository : IAzureSubscriptionRepository
    {
        private readonly List<AzureSubscription> _subscriptions = [];

        public Task<bool> ExistsByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken)
            => Task.FromResult(_subscriptions.Any(subscription => subscription.ExternalSubscriptionId == externalSubscriptionId));

        public Task AddAsync(AzureSubscription subscription, CancellationToken cancellationToken)
        {
            _subscriptions.Add(subscription);
            return Task.CompletedTask;
        }

        public Task<AzureSubscription?> GetFirstAsync(CancellationToken cancellationToken)
            => Task.FromResult(_subscriptions.FirstOrDefault());

        public Task DeleteAsync(AzureSubscription subscription, CancellationToken cancellationToken)
        {
            _subscriptions.Remove(subscription);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
