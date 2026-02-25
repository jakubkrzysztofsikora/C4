using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Application.Adapters;

public sealed class TerraformParser : IIacStateParser
{
    public Task<IReadOnlyCollection<IacResourceRecord>> ParseAsync(string iacContent, string format, CancellationToken cancellationToken)
    {
        if (!string.Equals(format, "terraform", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(format, "tf", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<IReadOnlyCollection<IacResourceRecord>>([]);
        }

        var lines = iacContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var resources = lines
            .Where(line => line.StartsWith("resource ", StringComparison.OrdinalIgnoreCase))
            .Select((line, idx) => new IacResourceRecord($"tf-{idx}", "terraform/resource", line))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IacResourceRecord>>(resources);
    }
}
