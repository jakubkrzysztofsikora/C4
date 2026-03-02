using C4.Modules.Discovery.Application.Ports;
using System.Text.RegularExpressions;

namespace C4.Modules.Discovery.Application.Adapters;

public sealed class BicepParser : IIacStateParser
{
    private static readonly Regex ResourceDeclarationRegex = new(
        @"^resource\s+(?<symbol>[A-Za-z_][A-Za-z0-9_]*)\s+'(?<type>[^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NameAssignmentRegex = new(
        @"^\s*name\s*:\s*'(?<name>[^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<IReadOnlyCollection<IacResourceRecord>> ParseAsync(string iacContent, string format, CancellationToken cancellationToken)
    {
        if (!string.Equals(format, "bicep", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<IReadOnlyCollection<IacResourceRecord>>([]);
        }

        var lines = iacContent.Split('\n');
        List<IacResourceRecord> resources = [];

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var declarationMatch = ResourceDeclarationRegex.Match(line);
            if (!declarationMatch.Success)
                continue;

            var symbol = declarationMatch.Groups["symbol"].Value;
            var rawType = declarationMatch.Groups["type"].Value;
            var armType = rawType.Split('@', 2)[0];

            var name = symbol;
            for (var j = i + 1; j < lines.Length; j++)
            {
                var next = lines[j].Trim();
                if (next.StartsWith("resource ", StringComparison.OrdinalIgnoreCase))
                    break;

                var nameMatch = NameAssignmentRegex.Match(next);
                if (nameMatch.Success)
                {
                    name = nameMatch.Groups["name"].Value;
                    break;
                }
            }

            var normalizedType = armType.Trim();
            if (normalizedType.Length == 0)
                normalizedType = "bicep/resource";

            var resourceId = $"/providers/{normalizedType}/{name}".ToLowerInvariant();
            resources.Add(new IacResourceRecord(resourceId, normalizedType, line));
        }

        return Task.FromResult<IReadOnlyCollection<IacResourceRecord>>(resources);
    }
}
