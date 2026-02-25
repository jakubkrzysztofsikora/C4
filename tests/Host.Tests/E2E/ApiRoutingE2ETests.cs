using System.Net;
using System.Net.Http.Json;
using C4.Shared.Testing;

namespace C4.Host.Tests.E2E;

[Trait("Category", "E2E")]
public sealed class ApiRoutingE2ETests : IClassFixture<C4WebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiRoutingE2ETests(C4WebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Theory]
    [InlineData("GET", "/api/projects/{0}/graph")]
    [InlineData("GET", "/api/projects/{0}/diagram")]
    [InlineData("GET", "/api/projects/{0}/view-presets")]
    [InlineData("GET", "/api/projects/{0}/telemetry/test-service/health")]
    public async Task Get_AllProjectScopedEndpoints_ReturnValidResponses(string method, string routeTemplate)
    {
        var projectId = Guid.NewGuid();
        var route = string.Format(routeTemplate, projectId);

        var response = method == "GET"
            ? await _client.GetAsync(route)
            : throw new InvalidOperationException();

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_IdentityOrganization_EndpointIsReachable()
    {
        var response = await _client.PostAsJsonAsync("/api/organizations", new { Name = $"E2E-{Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_DiscoverySubscription_EndpointIsReachable()
    {
        var response = await _client.PostAsJsonAsync("/api/discovery/subscriptions", new { ExternalSubscriptionId = "e2e-sub-001", DisplayName = "E2E Subscription" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_TelemetryIngest_EndpointIsReachable()
    {
        var projectId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/telemetry", new { Service = "e2e-service", Value = 99.9 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_VisualizationViewPreset_EndpointIsReachable()
    {
        var projectId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/view-presets", new { Name = "E2E Preset", Json = """{"layout":"e2e"}""" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
