using C4.Modules.Telemetry.Domain.Metrics;

namespace C4.Modules.Telemetry.Tests.Domain;

public sealed class ServiceHealthStatusExtensionsTests
{
    [Fact]
    public void FromScore_ScoreAbove08_ReturnsGreen()
    {
        double score = 0.9;

        ServiceHealthStatus result = ServiceHealthStatusExtensions.FromScore(score);

        result.Should().Be(ServiceHealthStatus.Green);
    }

    [Fact]
    public void FromScore_ScoreExactly08_ReturnsGreen()
    {
        double score = 0.8;

        ServiceHealthStatus result = ServiceHealthStatusExtensions.FromScore(score);

        result.Should().Be(ServiceHealthStatus.Green);
    }

    [Fact]
    public void FromScore_ScoreAbove05_ReturnsYellow()
    {
        double score = 0.6;

        ServiceHealthStatus result = ServiceHealthStatusExtensions.FromScore(score);

        result.Should().Be(ServiceHealthStatus.Yellow);
    }

    [Fact]
    public void FromScore_ScoreExactly05_ReturnsYellow()
    {
        double score = 0.5;

        ServiceHealthStatus result = ServiceHealthStatusExtensions.FromScore(score);

        result.Should().Be(ServiceHealthStatus.Yellow);
    }

    [Fact]
    public void FromScore_ScoreBelow05_ReturnsRed()
    {
        double score = 0.3;

        ServiceHealthStatus result = ServiceHealthStatusExtensions.FromScore(score);

        result.Should().Be(ServiceHealthStatus.Red);
    }

    [Fact]
    public void FromScore_Zero_ReturnsRed()
    {
        double score = 0.0;

        ServiceHealthStatus result = ServiceHealthStatusExtensions.FromScore(score);

        result.Should().Be(ServiceHealthStatus.Red);
    }
}
