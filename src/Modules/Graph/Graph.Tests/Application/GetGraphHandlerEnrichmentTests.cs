using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Graph.Tests.Application;

public sealed class GetGraphHandlerEnrichmentTests
{
    [Fact]
    public async Task Handle_WithTelemetry_ReturnsEnrichedNodes()
    {
        var projectId = Guid.NewGuid();
        var graph = ArchitectureGraph.Create(projectId);
        graph.AddOrUpdateNode("/r/api", "OrderApi", Domain.C4Level.Container);
        var repository = new FakeRepository(graph);
        var telemetry = new FakeTelemetryQueryService(
        [
            new ServiceHealthSummary("OrderApi", 0.9, "green")
        ]);
        var handler = new GetGraphHandler(repository, telemetry, new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(projectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var node = result.Value.Nodes.Single();
        node.Health.Should().Be("green");
        node.HealthScore.Should().Be(0.9);
    }

    [Fact]
    public async Task Handle_NoTelemetry_ReturnsDefaultHealth()
    {
        var projectId = Guid.NewGuid();
        var graph = ArchitectureGraph.Create(projectId);
        graph.AddOrUpdateNode("/r/api", "OrderApi", Domain.C4Level.Container);
        var repository = new FakeRepository(graph);
        var telemetry = new FakeTelemetryQueryService([]);
        var handler = new GetGraphHandler(repository, telemetry, new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(projectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var node = result.Value.Nodes.Single();
        node.Health.Should().Be("green");
        node.HealthScore.Should().Be(1.0);
    }

    [Fact]
    public async Task Handle_PartialTelemetry_EnrichesMatchingNodesOnly()
    {
        var projectId = Guid.NewGuid();
        var graph = ArchitectureGraph.Create(projectId);
        graph.AddOrUpdateNode("/r/api", "OrderApi", Domain.C4Level.Container);
        graph.AddOrUpdateNode("/r/db", "OrderDb", Domain.C4Level.Container);
        var repository = new FakeRepository(graph);
        var telemetry = new FakeTelemetryQueryService(
        [
            new ServiceHealthSummary("OrderApi", 0.4, "red")
        ]);
        var handler = new GetGraphHandler(repository, telemetry, new EmptyDriftQueryService(), new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetGraphQuery(projectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var enrichedNode = result.Value.Nodes.Single(n => n.Name == "OrderApi");
        var defaultNode = result.Value.Nodes.Single(n => n.Name == "OrderDb");
        enrichedNode.Health.Should().Be("red");
        enrichedNode.HealthScore.Should().Be(0.4);
        defaultNode.Health.Should().Be("green");
        defaultNode.HealthScore.Should().Be(1.0);
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var projectId = Guid.NewGuid();
        var graph = ArchitectureGraph.Create(projectId);
        var repository = new FakeRepository(graph);
        var handler = new GetGraphHandler(repository, new FakeTelemetryQueryService([]), new EmptyDriftQueryService(), new DenyingAuthorizationService());

        var result = await handler.Handle(new GetGraphQuery(projectId, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("authorization.denied");
    }

    private sealed class FakeRepository(ArchitectureGraph graph) : IArchitectureGraphRepository
    {
        public Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<ArchitectureGraph?>(graph.ProjectId == projectId ? graph : null);

        public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTelemetryQueryService(IReadOnlyCollection<ServiceHealthSummary> summaries) : ITelemetryQueryService
    {
        public Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(summaries);
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

    private sealed class DenyingAuthorizationService : IProjectAuthorizationService
    {
        public Task<Result<bool>> AuthorizeAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Failure(new Error("authorization.denied", "Access denied.")));

        public Task<Result<bool>> AuthorizeOwnerAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Failure(new Error("authorization.denied", "Access denied.")));
    }
}
