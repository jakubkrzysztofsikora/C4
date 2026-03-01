using C4.Modules.Graph.Application.GetThreatAssessment;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.ArchitectureGraph;
using C4.Shared.Kernel;

namespace C4.Modules.Graph.Tests.Application;

public sealed class GetThreatAssessmentHandlerTests
{
    [Fact]
    public async Task Handle_ExistingGraph_ReturnsThreatAssessment()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "PublicApi", Domain.C4Level.Container);
        var repository = new FakeRepository(graph);
        var expectedThreats = new[] { new ThreatItem("PublicApi", "Injection", "High", "Use parameterized queries") };
        var detector = new FakeThreatDetector(new ThreatDetectionResult("High", expectedThreats));
        var handler = new GetThreatAssessmentHandler(repository, detector, new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetThreatAssessmentQuery(graph.ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("High");
        result.Value.Threats.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NonExistentGraph_ReturnsFailure()
    {
        var repository = new FakeRepository(null);
        var detector = new FakeThreatDetector(new ThreatDetectionResult("None", []));
        var handler = new GetThreatAssessmentHandler(repository, detector, new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetThreatAssessmentQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("graph.not_found");
    }

    [Fact]
    public async Task Handle_ExistingGraph_PassesNodeAndEdgeDescriptionsToDetector()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        var node1 = graph.AddOrUpdateNode("/r/1", "Gateway", Domain.C4Level.Container);
        var node2 = graph.AddOrUpdateNode("/r/2", "Backend", Domain.C4Level.Container);
        graph.AddEdge(node1, node2);
        var repository = new FakeRepository(graph);
        var capturingDetector = new CapturingThreatDetector();
        var handler = new GetThreatAssessmentHandler(repository, capturingDetector, new AlwaysAuthorizingService());

        await handler.Handle(new GetThreatAssessmentQuery(graph.ProjectId), CancellationToken.None);

        capturingDetector.CapturedNodesDescription.Should().Contain("Gateway (Container)");
        capturingDetector.CapturedEdgesDescription.Should().Contain("->");
    }

    [Fact]
    public async Task Handle_ExistingGraph_MapsResponseProjectId()
    {
        var graph = ArchitectureGraph.Create(Guid.NewGuid());
        graph.AddOrUpdateNode("/r/1", "Service", Domain.C4Level.Component);
        var repository = new FakeRepository(graph);
        var detector = new FakeThreatDetector(new ThreatDetectionResult("Low", []));
        var handler = new GetThreatAssessmentHandler(repository, detector, new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetThreatAssessmentQuery(graph.ProjectId), CancellationToken.None);

        result.Value.ProjectId.Should().Be(graph.ProjectId);
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var repository = new FakeRepository(null);
        var detector = new FakeThreatDetector(new ThreatDetectionResult("None", []));
        var handler = new GetThreatAssessmentHandler(repository, detector, new DenyingAuthorizationService());

        var result = await handler.Handle(new GetThreatAssessmentQuery(Guid.NewGuid()), CancellationToken.None);

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

    private sealed class FakeThreatDetector(ThreatDetectionResult result) : IThreatDetector
    {
        public Task<ThreatDetectionResult> DetectThreatsAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class CapturingThreatDetector : IThreatDetector
    {
        public string CapturedNodesDescription { get; private set; } = string.Empty;
        public string CapturedEdgesDescription { get; private set; } = string.Empty;

        public Task<ThreatDetectionResult> DetectThreatsAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
        {
            CapturedNodesDescription = nodesDescription;
            CapturedEdgesDescription = edgesDescription;
            return Task.FromResult(new ThreatDetectionResult("None", []));
        }
    }
}
