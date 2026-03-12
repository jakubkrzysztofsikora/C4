using C4.Modules.Telemetry.Infrastructure.Persistence;
using C4.Modules.Telemetry.Infrastructure.Repositories;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Telemetry.Tests.Infrastructure;

public sealed class AppInsightsConfigStoreCredentialTests
{
    [Fact]
    public async Task StoreAsync_SeparateApiKeyAndInstrumentationKey_BothPersisted()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-credential-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var store = new AppInsightsConfigStore(dbContext);
        var projectId = Guid.NewGuid();

        await store.StoreAsync(projectId, "my-app", "ikey-abc-123", "api-key-xyz-456", CancellationToken.None);

        var config = await store.GetAsync(projectId, CancellationToken.None);

        using var scope = new AssertionScope();
        config.Should().NotBeNull();
        config!.InstrumentationKey.Should().Be("ikey-abc-123");
        config.ApiKey.Should().Be("api-key-xyz-456");
    }

    [Fact]
    public async Task StoreAsync_BlankApiKey_DoesNotOverwriteExisting()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-credential-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var store = new AppInsightsConfigStore(dbContext);
        var projectId = Guid.NewGuid();

        await store.StoreAsync(projectId, "my-app", "ikey-abc-123", "api-key-original", CancellationToken.None);
        await store.StoreAsync(projectId, "my-app", string.Empty, string.Empty, CancellationToken.None);

        var config = await store.GetAsync(projectId, CancellationToken.None);

        config!.ApiKey.Should().Be("api-key-original");
    }

    [Fact]
    public async Task StoreAsync_NewApiKey_OverwritesPrevious()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-credential-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var store = new AppInsightsConfigStore(dbContext);
        var projectId = Guid.NewGuid();

        await store.StoreAsync(projectId, "my-app", "ikey-1", "api-key-first", CancellationToken.None);
        await store.StoreAsync(projectId, "my-app", "ikey-2", "api-key-second", CancellationToken.None);

        var config = await store.GetAsync(projectId, CancellationToken.None);

        config!.ApiKey.Should().Be("api-key-second");
    }

    [Fact]
    public async Task GetAsync_NoRecord_ReturnsNull()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-credential-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var store = new AppInsightsConfigStore(dbContext);

        var config = await store.GetAsync(Guid.NewGuid(), CancellationToken.None);

        config.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ExistingRecord_ReturnsApiKeyField()
    {
        var dbOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase($"telemetry-credential-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TelemetryDbContext(dbOptions);
        var store = new AppInsightsConfigStore(dbContext);
        var projectId = Guid.NewGuid();

        await store.StoreAsync(projectId, "my-app", "ikey-abc", "api-key-stored", CancellationToken.None);

        var config = await store.GetAsync(projectId, CancellationToken.None);

        config!.ApiKey.Should().Be("api-key-stored");
    }
}
