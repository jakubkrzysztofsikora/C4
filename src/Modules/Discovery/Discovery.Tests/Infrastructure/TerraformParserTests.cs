using C4.Modules.Discovery.Application.Adapters;

namespace C4.Modules.Discovery.Tests.Infrastructure;

public sealed class TerraformParserTests
{
    [Fact]
    public async Task Parse_ResourceLines_ReturnsRecords()
    {
        var parser = new TerraformParser();
        var content = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "sample.tf"));

        var result = await parser.ParseAsync(content, "terraform", CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
