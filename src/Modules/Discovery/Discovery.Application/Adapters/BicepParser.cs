using C4.Modules.Discovery.Application.Ports;
using System.Text.RegularExpressions;

namespace C4.Modules.Discovery.Application.Adapters;

public sealed class BicepParser : IIacStateParser
{
    private static readonly Regex ResourceDeclarationRegex = new(
        @"^resource\s+(?<symbol>[A-Za-z_][A-Za-z0-9_]*)\s+'(?<type>[^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NameAssignmentRegex = new(
        @"^\s*name\s*:\s*(?<value>[^,\r\n]+)",
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
                    var parsedName = TryExtractNameLiteral(nameMatch.Groups["value"].Value);
                    if (!string.IsNullOrWhiteSpace(parsedName))
                        name = parsedName!;
                    break;
                }
            }

            var normalizedType = armType.Trim();
            if (normalizedType.Length == 0)
                normalizedType = "bicep/resource";

            var resourceId = $"/providers/{normalizedType}/{name}".ToLowerInvariant();
            resources.Add(new IacResourceRecord(resourceId, normalizedType, name));
        }

        return Task.FromResult<IReadOnlyCollection<IacResourceRecord>>(resources);
    }

    private static string? TryExtractNameLiteral(string rawValue)
    {
        var value = rawValue.Trim();
        if (value.Length < 2)
            return null;

        if ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"'))
            return value[1..^1];

        if (value.StartsWith("'", StringComparison.Ordinal))
        {
            var end = value.IndexOf('\'', 1);
            return end > 1 ? value[1..end] : null;
        }

        if (value.StartsWith("\"", StringComparison.Ordinal))
        {
            var end = value.IndexOf('"', 1);
            return end > 1 ? value[1..end] : null;
        }

        return null;
    }
}
