namespace C4.Modules.Graph.Application.GetGraph;

public static class EnvironmentClassifier
{
    private static readonly HashSet<string> EnvironmentTagKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "environment",
        "env",
        "stage",
        "slot"
    };

    private static readonly (string Keyword, string Environment)[] EnvironmentPatterns =
    [
        ("nonprod", "nonprod"),
        ("preprod", "nonprod"),
        ("prod", "production"),
        ("prd", "production"),
        ("staging", "staging"),
        ("stage", "staging"),
        ("stg", "staging"),
        ("ppe", "staging"),
        ("dev", "development"),
        ("qa", "qa"),
        ("uat", "qa"),
        ("test", "test"),
        ("demo", "demo"),
        ("e2e", "e2e"),
        ("trial", "trial"),
        ("sandbox", "sandbox"),
    ];

    public static string InferEnvironment(string resourceName, string? resourceGroup = null, IReadOnlyCollection<string>? tags = null)
    {
        if (TryInferFromTags(tags, out var fromTags))
            return fromTags;

        var lower = $"{resourceGroup} {resourceName}".ToLowerInvariant();
        var fromName = InferFromText(lower);
        return fromName;
    }

    private static bool TryInferFromTags(IReadOnlyCollection<string>? tags, out string environment)
    {
        environment = "unknown";
        if (tags is null || tags.Count == 0)
            return false;

        foreach (var rawTag in tags)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
                continue;

            var tag = rawTag.Trim();
            var separatorIndex = tag.IndexOf(':');
            if (separatorIndex > 0 && separatorIndex < tag.Length - 1)
            {
                var key = tag[..separatorIndex].Trim();
                var value = tag[(separatorIndex + 1)..].Trim();
                if (EnvironmentTagKeys.Contains(key))
                {
                    var inferred = InferFromText(value.ToLowerInvariant());
                    if (!inferred.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        environment = inferred;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string InferFromText(string lower)
    {
        foreach (var (keyword, environment) in EnvironmentPatterns)
        {
            if (ContainsSegment(lower, keyword))
                return environment;
        }

        return "unknown";
    }

    private static bool ContainsSegment(string input, string segment)
    {
        var index = 0;
        while (true)
        {
            index = input.IndexOf(segment, index, StringComparison.Ordinal);
            if (index < 0)
                return false;

            var before = index == 0 || !char.IsLetterOrDigit(input[index - 1]);
            var afterPos = index + segment.Length;
            var after = afterPos >= input.Length || !char.IsLetterOrDigit(input[afterPos]);

            if (before && after)
                return true;

            index += segment.Length;
        }
    }
}
