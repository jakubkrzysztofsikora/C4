using C4.Modules.Discovery.Application.Adapters;

namespace C4.Modules.Discovery.Tests.Infrastructure;

public sealed class BicepParserTests
{
    [Fact]
    public async Task Parse_ResourceLines_ReturnsRecords()
    {
        var parser = new BicepParser();
        var content = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "sample.bicep"));

        var result = await parser.ParseAsync(content, "bicep", CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
