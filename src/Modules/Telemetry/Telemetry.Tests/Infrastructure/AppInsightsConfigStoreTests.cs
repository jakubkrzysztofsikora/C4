using C4.Modules.Telemetry.Infrastructure.Persistence;
using C4.Modules.Telemetry.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Telemetry.Tests.Infrastructure;

public sealed class AppInsightsConfigStoreTests
{
    [Fact]
    public async Task StoreAsync_DoesNotClearExistingKey_WhenIncomingKeyIsBlank()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-config-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var store = new AppInsightsConfigStore(dbContext);
        var projectId = Guid.NewGuid();

        await store.StoreAsync(projectId, "app-a", "ikey-1", "api-key-1", CancellationToken.None);
        await store.StoreAsync(projectId, "app-b", string.Empty, string.Empty, CancellationToken.None);

        var config = await store.GetAsync(projectId, CancellationToken.None);

        config.Should().NotBeNull();
        config!.AppId.Should().Contain("app-a");
        config.AppId.Should().Contain("app-b");
        config.InstrumentationKey.Should().Be("ikey-1");
        config.ApiKey.Should().Be("api-key-1");
    }

    [Fact]
    public async Task StoreAsync_UpdatesExistingKey_WhenIncomingKeyIsProvided()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-config-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var store = new AppInsightsConfigStore(dbContext);
        var projectId = Guid.NewGuid();

        await store.StoreAsync(projectId, "app-a", "ikey-1", "api-key-1", CancellationToken.None);
        await store.StoreAsync(projectId, "app-a", "ikey-2", "api-key-2", CancellationToken.None);

        var config = await store.GetAsync(projectId, CancellationToken.None);

        config.Should().NotBeNull();
        config!.InstrumentationKey.Should().Be("ikey-2");
        config.ApiKey.Should().Be("api-key-2");
    }
}
