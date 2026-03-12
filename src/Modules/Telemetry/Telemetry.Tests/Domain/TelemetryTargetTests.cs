using C4.Modules.Telemetry.Domain;
using FluentAssertions.Execution;

namespace C4.Modules.Telemetry.Tests.Domain;

public sealed class TelemetryTargetTests
{
    [Fact]
    public void TelemetryTarget_Creation_SetsAllProperties()
    {
        var metadata = new Dictionary<string, string>
        {
            ["appId"] = "my-app-id",
            ["apiKey"] = "my-api-key"
        };

        var target = new TelemetryTarget(
            "my-app-id",
            TelemetryProvider.ApplicationInsights,
            AuthMode.ApiKey,
            metadata);

        using var scope = new AssertionScope();
        target.Id.Should().Be("my-app-id");
        target.Provider.Should().Be(TelemetryProvider.ApplicationInsights);
        target.AuthMode.Should().Be(AuthMode.ApiKey);
        target.ConnectionMetadata.Should().ContainKey("appId").WhoseValue.Should().Be("my-app-id");
        target.ConnectionMetadata.Should().ContainKey("apiKey").WhoseValue.Should().Be("my-api-key");
    }

    [Fact]
    public void TelemetryProvider_Values_MatchExpectedProviders()
    {
        var values = Enum.GetValues<TelemetryProvider>();

        values.Should().Contain(TelemetryProvider.ApplicationInsights);
    }

    [Fact]
    public void AuthMode_Values_MatchExpectedModes()
    {
        var values = Enum.GetValues<AuthMode>();

        using var scope = new AssertionScope();
        values.Should().Contain(AuthMode.ApiKey);
        values.Should().Contain(AuthMode.ClientCredentials);
        values.Should().Contain(AuthMode.Delegated);
    }
}
