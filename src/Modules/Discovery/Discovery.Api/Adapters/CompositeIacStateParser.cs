using C4.Modules.Discovery.Application.Adapters;
using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class CompositeIacStateParser(BicepParser bicepParser, TerraformParser terraformParser) : IIacStateParser
{
    public Task<IReadOnlyCollection<IacResourceRecord>> ParseAsync(string iacContent, string format, CancellationToken cancellationToken)
    {
        if (string.Equals(format, "bicep", StringComparison.OrdinalIgnoreCase))
        {
            return bicepParser.ParseAsync(iacContent, format, cancellationToken);
        }

        if (string.Equals(format, "terraform", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "tf", StringComparison.OrdinalIgnoreCase))
        {
            return terraformParser.ParseAsync(iacContent, format, cancellationToken);
        }

        throw new ArgumentException($"Unsupported IaC format: {format}", nameof(format));
    }
}
