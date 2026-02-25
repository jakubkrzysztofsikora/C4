using C4.Modules.Telemetry.Application.GetServiceHealth;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;

namespace C4.Modules.Telemetry.Tests.Application;

public sealed class GetServiceHealthHandlerTests
{
    [Fact]
    public async Task Handle_ExistingHealth_ReturnsValue()
    {
        var repo = new FakeRepository();
        var handler = new GetServiceHealthHandler(repo);

        var result = await handler.Handle(new GetServiceHealthQuery(Guid.Parse("11111111-1111-1111-1111-111111111111"), "api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Green");
    }

    private sealed class FakeRepository : ITelemetryRepository
    {
        public Task AddMetricAsync(MetricDataPoint metric, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ServiceHealth?> GetServiceHealthAsync(Guid projectId, string service, CancellationToken cancellationToken)
            => Task.FromResult<ServiceHealth?>(new ServiceHealth(projectId, service, .9, ServiceHealthStatus.Green, DateTime.UtcNow));
    }
}
