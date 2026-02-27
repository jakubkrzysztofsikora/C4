using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
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
        var handler = new GetGraphHandler(repo, telemetry);

        var result = await handler.Handle(new GetGraphQuery(graph.ProjectId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Should().HaveCount(1);
    }

    private sealed class FakeRepository(ArchitectureGraph graph) : IArchitectureGraphRepository
    {
        public Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<ArchitectureGraph?>(graph.ProjectId == projectId ? graph : null);

        public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class EmptyTelemetryQueryService : ITelemetryQueryService
    {
        public Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ServiceHealthSummary>>([]);
    }
}
