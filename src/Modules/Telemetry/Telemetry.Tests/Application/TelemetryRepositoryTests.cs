using C4.Modules.Telemetry.Domain.Metrics;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using C4.Modules.Telemetry.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Telemetry.Tests.Application;

public sealed class TelemetryRepositoryTests
{
    [Fact]
    public async Task GetServiceHealthAsync_AveragesStoredMetrics()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var repository = new TelemetryRepository(dbContext);
        var projectId = Guid.NewGuid();

        await repository.AddMetricAsync(new MetricDataPoint(projectId, "api", 0.9, DateTime.UtcNow), CancellationToken.None);
        await repository.AddMetricAsync(new MetricDataPoint(projectId, "api", 0.5, DateTime.UtcNow), CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var health = await repository.GetServiceHealthAsync(projectId, "api", CancellationToken.None);

        health.Should().NotBeNull();
        health!.Score.Should().BeApproximately(0.7, 0.0001);
        health.Status.Should().Be(ServiceHealthStatus.Yellow);
    }
}
