using System.Net;
using System.Net.Http.Json;
using C4.Shared.Testing;

namespace C4.Host.Tests.E2E;

[Trait("Category", "E2E")]
public sealed class ModuleIsolationE2ETests : IClassFixture<C4WebApplicationFactory>
{
    private readonly HttpClient _client;

    public ModuleIsolationE2ETests(C4WebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Identity_RegisterOrganization_ReturnsCreatedWithLocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/organizations", new { Name = $"Isolation-{Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/organizations/");
    }

    [Fact]
    public async Task Discovery_ConnectSubscription_ReturnsCreatedWithId()
    {
        var uniqueId = $"iso-sub-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/discovery/subscriptions", new { ExternalSubscriptionId = uniqueId, DisplayName = "Isolation Sub" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        body!.SubscriptionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Telemetry_IngestMetric_ReturnsOk()
    {
        var projectId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/telemetry", new { Service = "isolation-svc", Value = 88.0 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Visualization_SaveViewPreset_ReturnsCreated()
    {
        var projectId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/view-presets", new { Name = "Isolation Preset", Json = """{"zoom":1.0}""" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Graph_GetGraph_ForUnknownProject_Returns404()
    {
        var projectId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/projects/{projectId}/graph");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AllModules_HandleConcurrentRequests_WithoutInterference()
    {
        var projectId = Guid.NewGuid();

        var tasks = new[]
        {
            _client.PostAsJsonAsync("/api/organizations", new { Name = $"Concurrent-{Guid.NewGuid():N}" }),
            _client.PostAsJsonAsync("/api/discovery/subscriptions", new { ExternalSubscriptionId = $"conc-{Guid.NewGuid():N}", DisplayName = "Concurrent Sub" }),
            _client.PostAsJsonAsync($"/api/projects/{projectId}/telemetry", new { Service = "concurrent-svc", Value = 50.0 }),
            _client.PostAsJsonAsync($"/api/projects/{projectId}/view-presets", new { Name = "Concurrent Preset", Json = """{"concurrent":true}""" }),
            _client.GetAsync($"/api/projects/{projectId}/graph")
        };

        var responses = await Task.WhenAll(tasks);

        responses.Should().AllSatisfy(r =>
            r.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Created,
                HttpStatusCode.NotFound));
    }

    private sealed record SubscriptionResponse(Guid SubscriptionId, string DisplayName);
}
