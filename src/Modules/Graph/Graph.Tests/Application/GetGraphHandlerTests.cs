using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Graph.Tests.Application;

public sealed class GetGraphHandlerTests
{
    [Fact]
    public async Task Handle_ExistingGraph_ReturnsNodes()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "Api", Domain.C4Level.Container);
        var repo = new FakeRepository(graph);
        var telemetry = new EmptyTelemetryQueryService();
        var drift = new EmptyDriftQueryService();
        var handler = new GetGraphHandler(repo, telemetry, drift, new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(graph.ProjectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NodeWithServiceType_DtoIncludesServiceType()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "Api", Domain.C4Level.Container, "api");
        var repo = new FakeRepository(graph);
        var handler = new GetGraphHandler(repo, new EmptyTelemetryQueryService(), new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(graph.ProjectId, null), CancellationToken.None);

        result.Value.Nodes.Single().ServiceType.Should().Be("api");
    }

    [Fact]
    public async Task Handle_NodeWithUnknownTechnology_DefaultsToExternalServiceType()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "Legacy", Domain.C4Level.Container);
        var repo = new FakeRepository(graph);
        var handler = new GetGraphHandler(repo, new EmptyTelemetryQueryService(), new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(graph.ProjectId, null), CancellationToken.None);

        result.Value.Nodes.Single().ServiceType.Should().Be("external");
    }

    [Fact]
    public async Task Handle_NodeWithResourceGroupInId_DtoIncludesResourceGroup()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/subscriptions/1/resourceGroups/my-rg/providers/Microsoft.Web/sites/app1", "app1", Domain.C4Level.Container, "app");
        var repo = new FakeRepository(graph);
        var handler = new GetGraphHandler(repo, new EmptyTelemetryQueryService(), new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(graph.ProjectId, null), CancellationToken.None);

        result.Value.Nodes.Single().ResourceGroup.Should().Be("my-rg");
    }

    [Fact]
    public async Task Handle_LegacyContainerNode_IsReleveledToComponentFromArmType()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode(
            "/subscriptions/1/resourceGroups/rg-prod/providers/Microsoft.Network/networkInterfaces/nic-1",
            "nic-1",
            Domain.C4Level.Container,
            "external");
        var repo = new FakeRepository(graph);
        var handler = new GetGraphHandler(repo, new EmptyTelemetryQueryService(), new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(graph.ProjectId, "Component"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Should().ContainSingle();
        result.Value.Nodes.Single().Level.Should().Be("Component");
    }

    [Fact]
    public async Task Handle_ContainerLevel_AutoInfraFilter_HidesInfrastructure()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode(
            "/subscriptions/1/resourceGroups/rg-prod/providers/Microsoft.Network/networkInterfaces/nic-1",
            "nic-1",
            Domain.C4Level.Container,
            "external");
        var repo = new FakeRepository(graph);
        var handler = new GetGraphHandler(repo, new EmptyTelemetryQueryService(), new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(graph.ProjectId, "Container"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Should().BeEmpty();
    }

    private sealed class FakeRepository(ArchitectureGraph graph) : IArchitectureGraphRepository
    {
        public Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<ArchitectureGraph?>(graph.ProjectId == projectId ? graph : null);

        public Task<ArchitectureGraph?> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<ArchitectureGraph?>(graph.ProjectId == projectId ? graph : null);

        public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class EmptyTelemetryQueryService : ITelemetryQueryService
    {
        public Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ServiceHealthSummary>>([]);
    }

    private sealed class EmptyDriftQueryService : IDriftQueryService
    {
        public Task<IReadOnlyCollection<string>> GetDriftedResourceIdsAsync(IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<string>>([]);
    }

    private sealed class AlwaysAuthorizingService : IProjectAuthorizationService
    {
        public Task<Result<bool>> AuthorizeAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Success(true));

        public Task<Result<bool>> AuthorizeOwnerAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Success(true));
    }
}
