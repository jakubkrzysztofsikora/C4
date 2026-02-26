using System.Net;
using System.Net.Http.Json;
using C4.Shared.Testing;

namespace C4.Host.Tests.E2E;

[Trait("Category", "E2E")]
public sealed class LoginE2ETests : IClassFixture<C4WebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginE2ETests(C4WebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Login_InvalidCredentials_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "nonexistent@test.com", Password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "", Password = "password123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "test@test.com", Password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
