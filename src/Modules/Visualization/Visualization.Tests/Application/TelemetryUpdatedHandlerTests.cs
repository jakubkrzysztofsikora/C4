using System.Text.Json;
using C4.Modules.Visualization.Application.IntegrationEventHandlers;
using C4.Modules.Visualization.Application.Ports;
using C4.Shared.Kernel.IntegrationEvents;

namespace C4.Modules.Visualization.Tests.Application;

public sealed class TelemetryUpdatedHandlerTests
{
    [Fact]
    public async Task Handle_ValidEvent_CallsNotifier()
    {
        var projectId = Guid.NewGuid();
        var services = new[] { new TelemetryUpdatedServiceItem("api", 0.95, "healthy") };
        var notification = new TelemetryUpdatedIntegrationEvent(projectId, services);
        var notifier = new FakeDiagramNotifier();
        var handler = new TelemetryUpdatedHandler(notifier);

        await handler.Handle(notification, CancellationToken.None);

        notifier.HealthOverlayCalls.Should().HaveCount(1);
        notifier.HealthOverlayCalls[0].ProjectId.Should().Be(projectId);
        notifier.HealthOverlayCalls[0].HealthJson.Should().Contain("api");
    }

    [Fact]
    public async Task Handle_MultipleServices_SerializesAll()
    {
        var projectId = Guid.NewGuid();
        var services = new[]
        {
            new TelemetryUpdatedServiceItem("api", 0.95, "healthy"),
            new TelemetryUpdatedServiceItem("db", 0.50, "degraded"),
            new TelemetryUpdatedServiceItem("cache", 0.10, "critical")
        };
        var notification = new TelemetryUpdatedIntegrationEvent(projectId, services);
        var notifier = new FakeDiagramNotifier();
        var handler = new TelemetryUpdatedHandler(notifier);

        await handler.Handle(notification, CancellationToken.None);

        string healthJson = notifier.HealthOverlayCalls[0].HealthJson;
        var deserialized = JsonSerializer.Deserialize<TelemetryUpdatedServiceItem[]>(healthJson);
        deserialized.Should().HaveCount(3);
        deserialized.Should().Contain(s => s.Service == "api");
        deserialized.Should().Contain(s => s.Service == "db");
        deserialized.Should().Contain(s => s.Service == "cache");
    }

    [Fact]
    public async Task Handle_EmptyServices_CallsNotifierWithEmptyArray()
    {
        var projectId = Guid.NewGuid();
        var notification = new TelemetryUpdatedIntegrationEvent(projectId, Array.Empty<TelemetryUpdatedServiceItem>());
        var notifier = new FakeDiagramNotifier();
        var handler = new TelemetryUpdatedHandler(notifier);

        await handler.Handle(notification, CancellationToken.None);

        notifier.HealthOverlayCalls.Should().HaveCount(1);
        notifier.HealthOverlayCalls[0].HealthJson.Should().Be("[]");
    }

    private sealed class FakeDiagramNotifier : IDiagramNotifier
    {
        public List<(Guid ProjectId, string HealthJson)> HealthOverlayCalls { get; } = [];

        public Task NotifyDiagramUpdatedAsync(Guid projectId, string diagramJson, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task NotifyHealthOverlayChangedAsync(Guid projectId, string healthJson, CancellationToken cancellationToken)
        {
            HealthOverlayCalls.Add((projectId, healthJson));
            return Task.CompletedTask;
        }
    }
}
