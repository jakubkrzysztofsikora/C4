using C4.Modules.Telemetry.Domain;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using C4.Modules.Telemetry.Infrastructure.Repositories;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Telemetry.Tests.Infrastructure;

public sealed class TelemetryTargetStoreTests
{
    [Fact]
    public async Task GetTargetsAsync_NoTargets_ReturnsEmpty()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-target-store-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var configStore = new AppInsightsConfigStore(dbContext);
        var store = new TelemetryTargetStore(configStore);

        var targets = await store.GetTargetsAsync(Guid.NewGuid(), CancellationToken.None);

        targets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTargetsAsync_WithConfigs_ReturnsMappedTargets()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-target-store-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var configStore = new AppInsightsConfigStore(dbContext);
        var store = new TelemetryTargetStore(configStore);
        var projectId = Guid.NewGuid();

        await configStore.StoreAsync(projectId, "app-alpha", "ikey-1", "api-key-1", CancellationToken.None);

        var targets = await store.GetTargetsAsync(projectId, CancellationToken.None);

        using var scope = new AssertionScope();
        targets.Should().ContainSingle();
        var target = targets.Single();
        target.Id.Should().Be("app-alpha");
        target.Provider.Should().Be(TelemetryProvider.ApplicationInsights);
        target.AuthMode.Should().Be(AuthMode.ApiKey);
        target.ConnectionMetadata.Should().ContainKey("appId").WhoseValue.Should().Be("app-alpha");
    }

    [Fact]
    public async Task UpsertTargetAsync_NewTarget_PersistsToUnderlyingStore()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-target-store-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var configStore = new AppInsightsConfigStore(dbContext);
        var store = new TelemetryTargetStore(configStore);
        var projectId = Guid.NewGuid();

        var target = new TelemetryTarget(
            "app-beta",
            TelemetryProvider.ApplicationInsights,
            AuthMode.ApiKey,
            new Dictionary<string, string>
            {
                ["appId"] = "app-beta",
                ["apiKey"] = "api-key-beta",
                ["instrumentationKey"] = "ikey-beta"
            });

        await store.UpsertTargetAsync(projectId, target, CancellationToken.None);

        var config = await configStore.GetAsync(projectId, CancellationToken.None);

        using var scope = new AssertionScope();
        config.Should().NotBeNull();
        config!.AppId.Should().Contain("app-beta");
        config.ApiKey.Should().Be("api-key-beta");
        config.InstrumentationKey.Should().Be("ikey-beta");
    }
}
