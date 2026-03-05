using C4.Modules.Graph.Application.GetGraph;

namespace C4.Modules.Graph.Tests.Application;

[Trait("Category", "Unit")]
public sealed class EnvironmentClassifierTests
{
    [Theory]
    [InlineData("circuit-prod-api", "production")]
    [InlineData("prod-gateway", "production")]
    [InlineData("api-prod", "production")]
    [InlineData("prod", "production")]
    public void InferEnvironment_ResourceNameContainsProdSegment_ReturnsProduction(string resourceName, string expected)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-stage-api", "staging")]
    [InlineData("stage-gateway", "staging")]
    [InlineData("api-stage", "staging")]
    public void InferEnvironment_ResourceNameContainsStageSegment_ReturnsStaging(string resourceName, string expected)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-staging-api", "staging")]
    [InlineData("staging-gateway", "staging")]
    [InlineData("api-staging", "staging")]
    public void InferEnvironment_ResourceNameContainsStagingSegment_ReturnsStaging(string resourceName, string expected)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-dev-api", "development")]
    [InlineData("dev-gateway", "development")]
    [InlineData("api-dev", "development")]
    public void InferEnvironment_ResourceNameContainsDevSegment_ReturnsDevelopment(string resourceName, string expected)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-test-api", "test")]
    [InlineData("test-gateway", "test")]
    [InlineData("api-test", "test")]
    public void InferEnvironment_ResourceNameContainsTestSegment_ReturnsTest(string resourceName, string expected)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-qa-api", "qa")]
    [InlineData("qa-gateway", "qa")]
    [InlineData("api-qa", "qa")]
    public void InferEnvironment_ResourceNameContainsQaSegment_ReturnsQa(string resourceName, string expected)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-app-api")]
    [InlineData("order-service")]
    [InlineData("my-app (App Service)")]
    public void InferEnvironment_ResourceNameHasNoEnvironmentKeyword_ReturnsUnknown(string resourceName)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be("unknown");
    }

    [Theory]
    [InlineData("production-api")]
    [InlineData("developer-portal")]
    [InlineData("stagecoach")]
    [InlineData("tester-service")]
    public void InferEnvironment_KeywordEmbeddedInsideLargerWord_ReturnsUnknown(string resourceName)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be("unknown");
    }

    [Fact]
    public void InferEnvironment_ResourceNameIsCaseMixedWithProdSegment_ReturnsProduction()
    {
        var result = EnvironmentClassifier.InferEnvironment("Circuit-PROD-Api");

        result.Should().Be("production");
    }

    [Fact]
    public void InferEnvironment_ProdAppearsAfterEmbeddedWordThenAsSegment_ReturnsProduction()
    {
        var result = EnvironmentClassifier.InferEnvironment("reproduced-app-prod-gateway");

        result.Should().Be("production");
    }

    [Fact]
    public void InferEnvironment_TagContainsEnvironmentValue_ReturnsEnvironmentFromTag()
    {
        var tags = new[] { "Environment:prod-eastus", "Owner:platform" };

        var result = EnvironmentClassifier.InferEnvironment("svc-main", "rg-main", tags);

        result.Should().Be("production");
    }

    [Fact]
    public void InferEnvironment_TagContainsUatValue_ReturnsQa()
    {
        var tags = new[] { "env:uat" };

        var result = EnvironmentClassifier.InferEnvironment("svc-main", "rg-main", tags);

        result.Should().Be("qa");
    }

    [Theory]
    [InlineData("my-ppe-api", "staging")]
    [InlineData("my-preprod-api", "nonprod")]
    [InlineData("my-prd-api", "production")]
    public void InferEnvironment_AdditionalEnvironmentAliasesAreSupported(string resourceName, string expected)
    {
        var result = EnvironmentClassifier.InferEnvironment(resourceName);

        result.Should().Be(expected);
    }
}
