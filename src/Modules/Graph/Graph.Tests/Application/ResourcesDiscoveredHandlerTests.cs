using C4.Shared.Kernel.IntegrationEvents;
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
                    new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "App Service", "app", "Container", true, null),
                    new DiscoveredResourceEventItem("/subscriptions/1/func", "Microsoft.Web/sites/functions", "func", "Function App", "app", "Component", true, null)
                ]),
            CancellationToken.None);

        repository.Graph.Should().NotBeNull();
        repository.Graph!.ProjectId.Should().Be(projectId);
        repository.Graph.Nodes.Should().HaveCount(2);
        repository.Graph.Snapshots.Should().HaveCount(1);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ResourcesWithIncludeInDiagramFalse_ExcludedFromGraph()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [
                    new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "App Service", "app", "Container", true, null),
                    new DiscoveredResourceEventItem("/subscriptions/1/nsg", "Microsoft.Network/networkSecurityGroups", "nsg", "NSG", "external", "Component", false, null)
                ]),
            CancellationToken.None);

        repository.Graph!.Nodes.Should().HaveCount(1);
        repository.Graph.Nodes.Single().ExternalResourceId.Should().Be("/subscriptions/1/web");
    }

    [Fact]
    public async Task Handle_ResourceWithFriendlyName_UsesFriendlyNameAsNodeLabel()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "My App Service", "app", "Container", true, null)]),
            CancellationToken.None);

        repository.Graph!.Nodes.Single().Name.Should().Be("web (My App Service)");
    }

    [Fact]
    public async Task Handle_ResourceWithNullFriendlyName_FallsBackToResourceName()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", null, null, null, true, null)]),
            CancellationToken.None);

        repository.Graph!.Nodes.Single().Name.Should().Be("web");
    }

    [Fact]
    public async Task Handle_ResourceWithComponentC4Level_CreatesNodeAtComponentLevel()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [new DiscoveredResourceEventItem("/subscriptions/1/func", "Microsoft.Web/sites/functions", "func", "Processor", "app", "Component", true, null)]),
            CancellationToken.None);

        repository.Graph!.Nodes.Single().Level.Should().Be(C4.Modules.Graph.Domain.C4Level.Component);
    }

    [Fact]
    public async Task Handle_ResourceWithParent_NodeHasParentId()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [
                    new DiscoveredResourceEventItem("/subscriptions/1/parent", "Microsoft.Web/sites", "parent", "Parent App", "app", "Container", true, null),
                    new DiscoveredResourceEventItem("/subscriptions/1/child", "Microsoft.Web/sites/slots", "child", "Child Slot", "app", "Component", true, "/subscriptions/1/parent")
                ]),
            CancellationToken.None);

        var childNode = repository.Graph!.Nodes.Single(n => n.ExternalResourceId == "/subscriptions/1/child");
        var parentNode = repository.Graph.Nodes.Single(n => n.ExternalResourceId == "/subscriptions/1/parent");
        childNode.ParentId.Should().Be(parentNode.Id);
    }

    [Fact]
    public async Task Handle_ResourceWithoutParent_NodeHasNoParent()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "App Service", "app", "Container", true, null)]),
            CancellationToken.None);

        repository.Graph!.Nodes.Single().ParentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ParentNotInBatch_NodeHasNoParent()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [new DiscoveredResourceEventItem("/subscriptions/1/child", "Microsoft.Web/sites/slots", "child", "Child Slot", "app", "Component", true, "/subscriptions/1/missing-parent")]),
            CancellationToken.None);

        repository.Graph!.Nodes.Single().ParentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DuplicateStableResources_PrefersHigherConfidenceRecord()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [
                    new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "Web Repo", "app", "Container", true, null, "repo", 0.8, null, "resource:web"),
                    new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "Web Azure", "app", "Container", true, null, "azure", 0.95, null, "resource:web")
                ]),
            CancellationToken.None);

        repository.Graph!.Nodes.Should().HaveCount(1);
        repository.Graph.Nodes.Single().Name.Should().Be("web (Web Azure)");
    }

    [Fact]
    public async Task Handle_SameConfidencePrefersAzureSource()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResourcesDiscoveredHandler(repository, unitOfWork);

        await handler.Handle(
            new ResourcesDiscoveredIntegrationEvent(
                projectId,
                [
                    new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "Repo Name", "app", "Container", true, null, "repo", 0.95, null, "resource:web"),
                    new DiscoveredResourceEventItem("/subscriptions/1/web", "Microsoft.Web/sites", "web", "Azure Name", "app", "Container", true, null, "azure", 0.95, null, "resource:web")
                ]),
            CancellationToken.None);

        repository.Graph!.Nodes.Should().HaveCount(1);
        repository.Graph.Nodes.Single().Name.Should().Be("web (Azure Name)");
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

        public Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
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
