using C4.Modules.Discovery.Application.IntegrationEvents;
using C4.Modules.Graph.Application.IntegrationEventHandlers;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
using C4.Shared.Kernel;

namespace C4.Modules.Graph.Tests.Application;

public sealed class ResourcesDiscoveredHandlerTests
{
    [Fact]
    public async Task Handle_CreatesGraphAndSnapshotFromDiscoveryEvent()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [
                    new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web"),
                    new DiscoveredResourceEventItem("/subscriptions/1/func", "Microsoft.Web/sites/functions", "func")
                ]),
            CancellationToken.None);

        repository.Graph.Should().NotBeNull();
        repository.Graph!.ProjectId.Should().Be(projectId);
        repository.Graph.Nodes.Should().HaveCount(2);
        repository.Graph.Snapshots.Should().HaveCount(1);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    private sealed class FakeRepository : IArchitectureGraphRepository
    {
        public ArchitectureGraph? Graph { get; private set; }

        public Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Graph is not null && Graph.ProjectId == projectId ? Graph : null);

        public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken)
        {
            Graph = graph;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
