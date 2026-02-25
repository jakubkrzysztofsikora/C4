using System.Net;
using System.Net.Http.Json;
using C4.Shared.Testing;

namespace C4.Host.Tests.E2E;

[Trait("Category", "E2E")]
public sealed class CrossModuleValidationE2ETests : IClassFixture<C4WebApplicationFactory>
{
    private readonly HttpClient _client;

    public CrossModuleValidationE2ETests(C4WebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task CreateProject_NonExistentOrganization_Returns400()
    {
        var fakeOrgId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/organizations/{fakeOrgId}/projects", new { Name = "Orphan Project" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGraph_NonExistentProject_Returns404()
    {
        var fakeProjectId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/projects/{fakeProjectId}/graph");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDiagram_AnyProject_ReturnsOk()
    {
        var projectId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/projects/{projectId}/diagram");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_NonExistentService_Returns404()
    {
        var fakeProjectId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/projects/{fakeProjectId}/telemetry/nonexistent/health");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegisterOrganization_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/organizations", new { Name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConnectSubscription_EmptyExternalId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/discovery/subscriptions", new { ExternalSubscriptionId = "", DisplayName = "Bad Sub" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportDiagram_InvalidFormat_ReturnsBadRequest()
    {
        var fakeProjectId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/projects/{fakeProjectId}/diagram/export?format=invalid");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
