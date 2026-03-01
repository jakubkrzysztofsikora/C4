using C4.Modules.Graph.Application.GetGraphDiff;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
using C4.Shared.Kernel;

namespace C4.Modules.Graph.Tests.Application;

public sealed class GetGraphDiffHandlerTests
{
    [Fact]
    public async Task Handle_TwoSnapshots_ReturnsAddedAndRemovedNodes()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "SharedService", Domain.C4Level.Container);
        graph.AddOrUpdateNode("/r/2", "OldService", Domain.C4Level.Container);
        var fromSnapshot = graph.CreateSnapshot();

        graph.AddOrUpdateNode("/r/3", "NewService", Domain.C4Level.Container);
        var toSnapshot = graph.CreateSnapshot();

        var repository = new FakeRepository(graph);
        var handler = new GetGraphDiffHandler(repository, new AlwaysAuthorizingService());

        var result = await handler.Handle(
            new GetGraphDiffQuery(graph.ProjectId, fromSnapshot.Id.Value, toSnapshot.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AddedNodes.Should().ContainSingle().Which.Should().Be("/r/3");
    }

    [Fact]
    public async Task Handle_TwoSnapshots_DetectsRemovedNodes()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "ServiceA", Domain.C4Level.Container);
        graph.AddOrUpdateNode("/r/2", "ServiceB", Domain.C4Level.Container);
        var fromSnapshot = graph.CreateSnapshot();

        graph.AddOrUpdateNode("/r/3", "ServiceC", Domain.C4Level.Container);
        var toSnapshot = graph.CreateSnapshot();

        var repository = new FakeRepository(graph);
        var handler = new GetGraphDiffHandler(repository, new AlwaysAuthorizingService());

        var result = await handler.Handle(
            new GetGraphDiffQuery(graph.ProjectId, toSnapshot.Id.Value, fromSnapshot.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RemovedNodes.Should().ContainSingle().Which.Should().Be("/r/3");
    }

    [Fact]
    public async Task Handle_NonExistentGraph_ReturnsFailure()
    {
        var repository = new FakeRepository(null);
        var handler = new GetGraphDiffHandler(repository, new AlwaysAuthorizingService());

        var result = await handler.Handle(
            new GetGraphDiffQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("graph.not_found");
    }

    [Fact]
    public async Task Handle_MissingSnapshot_ReturnsEmptyDiff()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "Service", Domain.C4Level.Container);
        graph.CreateSnapshot();
        var repository = new FakeRepository(graph);
        var handler = new GetGraphDiffHandler(repository, new AlwaysAuthorizingService());

        var result = await handler.Handle(
            new GetGraphDiffQuery(graph.ProjectId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AddedNodes.Should().BeEmpty();
        result.Value.RemovedNodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IdenticalSnapshots_ReturnsEmptyDiff()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "Service", Domain.C4Level.Container);
        var snapshot = graph.CreateSnapshot();
        var repository = new FakeRepository(graph);
        var handler = new GetGraphDiffHandler(repository, new AlwaysAuthorizingService());

        var result = await handler.Handle(
            new GetGraphDiffQuery(graph.ProjectId, snapshot.Id.Value, snapshot.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AddedNodes.Should().BeEmpty();
        result.Value.RemovedNodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var repository = new FakeRepository(null);
        var handler = new GetGraphDiffHandler(repository, new DenyingAuthorizationService());

        var result = await handler.Handle(
            new GetGraphDiffQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("authorization.denied");
    }

    private sealed class FakeRepository(ArchitectureGraph? graph) : IArchitectureGraphRepository
    {
        public Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(graph is not null && graph.ProjectId == projectId ? graph : null);

        public Task<ArchitectureGraph?> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(graph is not null && graph.ProjectId == projectId ? graph : null);

        public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
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
