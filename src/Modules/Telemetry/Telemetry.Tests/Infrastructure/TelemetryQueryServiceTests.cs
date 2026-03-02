using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Modules.Telemetry.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace C4.Modules.Telemetry.Tests.Infrastructure;

public sealed class TelemetryQueryServiceTests
{
    [Fact]
    public async Task GetServiceHealthSummariesAsync_WhenAppInsightsThrows_ReturnsPersistedHealthOnly()
    {
        var repo = new FakeTelemetryRepository(
            [
                new ServiceHealth(Guid.NewGuid(), "api", 0.9, ServiceHealthStatus.Green, DateTime.UtcNow)
            ]);
        var appInsights = new ThrowingApplicationInsightsClient();
        var service = new TelemetryQueryService(repo, appInsights, NullLogger<TelemetryQueryService>.Instance);

        var result = await service.GetServiceHealthSummariesAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().Service.Should().Be("api");
        result.Single().Score.Should().Be(0.9);
    }

    [Fact]
    public async Task GetServiceHealthSummariesAsync_SanitizesInvalidNumericValues()
    {
        var repo = new FakeTelemetryRepository([]);
        var appInsights = new FakeApplicationInsightsClient(
            [
                new ApplicationInsightsHealthRecord("worker", double.NaN, DateTime.UtcNow, -5, 2.0, double.NaN)
            ],
            []);
        var service = new TelemetryQueryService(repo, appInsights, NullLogger<TelemetryQueryService>.Instance);

        var result = await service.GetServiceHealthSummariesAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().ContainSingle();
        var summary = result.Single();
        summary.Service.Should().Be("worker");
        summary.Score.Should().Be(0);
        summary.Status.Should().Be("red");
        summary.RequestRate.Should().Be(0);
        summary.ErrorRate.Should().Be(1);
        summary.P95LatencyMs.Should().BeNull();
    }

    [Fact]
    public async Task GetDependencySummariesAsync_WhenAppInsightsThrows_ReturnsEmpty()
    {
        var repo = new FakeTelemetryRepository([]);
        var appInsights = new ThrowingApplicationInsightsClient();
        var service = new TelemetryQueryService(repo, appInsights, NullLogger<TelemetryQueryService>.Instance);

        var result = await service.GetDependencySummariesAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDependencySummariesAsync_SanitizesInvalidValues()
    {
        var repo = new FakeTelemetryRepository([]);
        var appInsights = new FakeApplicationInsightsClient(
            [],
            [
                new ApplicationInsightsDependencyRecord("api", "db", -11, double.NaN, -250, DateTime.UtcNow, "sql")
            ]);
        var service = new TelemetryQueryService(repo, appInsights, NullLogger<TelemetryQueryService>.Instance);

        var result = await service.GetDependencySummariesAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().ContainSingle();
        var dependency = result.Single();
        dependency.SourceService.Should().Be("api");
        dependency.TargetService.Should().Be("db");
        dependency.RequestRate.Should().Be(0);
        dependency.ErrorRate.Should().Be(0);
        dependency.P95LatencyMs.Should().Be(0);
    }

    private sealed class FakeTelemetryRepository(IReadOnlyCollection<ServiceHealth> serviceHealth) : ITelemetryRepository
    {
        public Task AddMetricAsync(MetricDataPoint metric, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<ServiceHealth?> GetServiceHealthAsync(Guid projectId, string service, CancellationToken cancellationToken)
            => Task.FromResult<ServiceHealth?>(serviceHealth.FirstOrDefault(s => s.Service.Equals(service, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyCollection<ServiceHealth>> GetAllServiceHealthAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(serviceHealth);
    }

    private sealed class FakeApplicationInsightsClient(
        IReadOnlyCollection<ApplicationInsightsHealthRecord> health,
        IReadOnlyCollection<ApplicationInsightsDependencyRecord> dependencies) : IApplicationInsightsClient
    {
        public Task<IReadOnlyCollection<ApplicationInsightsHealthRecord>> QueryServiceHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
            => Task.FromResult(health);

        public Task<IReadOnlyCollection<ApplicationInsightsDependencyRecord>> QueryDependencyHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
            => Task.FromResult(dependencies);
    }

    private sealed class ThrowingApplicationInsightsClient : IApplicationInsightsClient
    {
        public Task<IReadOnlyCollection<ApplicationInsightsHealthRecord>> QueryServiceHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
            => throw new HttpRequestException("boom");

        public Task<IReadOnlyCollection<ApplicationInsightsDependencyRecord>> QueryDependencyHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
            => throw new HttpRequestException("boom");
    }
}
