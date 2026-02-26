using C4.Modules.Graph.Application.AnalyzeArchitecture;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;

namespace C4.Modules.Graph.Tests.Application;

public sealed class AnalyzeArchitectureHandlerTests
{
    [Fact]
    public async Task Handle_ExistingGraph_ReturnsAnalysisWithSummaryAndRecommendations()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "WebApi", Domain.C4Level.Container);
        graph.AddOrUpdateNode("/r/2", "Database", Domain.C4Level.Container);
        var repository = new FakeRepository(graph);
        var expectedSummary = "Well-structured architecture";
        var expectedRecommendations = new[] { "Add caching layer" };
        var analyzer = new FakeAnalyzer(new ArchitectureAnalysisResult(expectedSummary, expectedRecommendations));
        var handler = new AnalyzeArchitectureHandler(repository, analyzer);

        var result = await handler.Handle(new AnalyzeArchitectureCommand(graph.ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new ArchitectureAnalysisResponse(graph.ProjectId, expectedSummary, expectedRecommendations));
    }

    [Fact]
    public async Task Handle_NonExistentGraph_ReturnsFailure()
    {
        var repository = new FakeRepository(null);
        var analyzer = new FakeAnalyzer(new ArchitectureAnalysisResult("", []));
        var handler = new AnalyzeArchitectureHandler(repository, analyzer);

        var result = await handler.Handle(new AnalyzeArchitectureCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("graph.not_found");
    }

    [Fact]
    public async Task Handle_ExistingGraph_PassesNodeAndEdgeDescriptionsToAnalyzer()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        var node1 = graph.AddOrUpdateNode("/r/1", "WebApi", Domain.C4Level.Container);
        var node2 = graph.AddOrUpdateNode("/r/2", "Database", Domain.C4Level.Component);
        graph.AddEdge(node1, node2);
        var repository = new FakeRepository(graph);
        var capturingAnalyzer = new CapturingAnalyzer();
        var handler = new AnalyzeArchitectureHandler(repository, capturingAnalyzer);

        await handler.Handle(new AnalyzeArchitectureCommand(graph.ProjectId), CancellationToken.None);

        capturingAnalyzer.CapturedNodesDescription.Should().Contain("WebApi (Container)");
        capturingAnalyzer.CapturedNodesDescription.Should().Contain("Database (Component)");
        capturingAnalyzer.CapturedEdgesDescription.Should().Contain("->");
    }

    private sealed class FakeRepository(ArchitectureGraph? graph) : IArchitectureGraphRepository
    {
        public Task<ArchitectureGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(graph is not null && graph.ProjectId == projectId ? graph : null);

        public Task UpsertAsync(ArchitectureGraph graph, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeAnalyzer(ArchitectureAnalysisResult result) : IArchitectureAnalyzer
    {
        public Task<ArchitectureAnalysisResult> AnalyzeAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class CapturingAnalyzer : IArchitectureAnalyzer
    {
        public string CapturedNodesDescription { get; private set; } = string.Empty;
        public string CapturedEdgesDescription { get; private set; } = string.Empty;

        public Task<ArchitectureAnalysisResult> AnalyzeAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
        {
            CapturedNodesDescription = nodesDescription;
            CapturedEdgesDescription = edgesDescription;
            return Task.FromResult(new ArchitectureAnalysisResult("summary", []));
        }
    }
}
