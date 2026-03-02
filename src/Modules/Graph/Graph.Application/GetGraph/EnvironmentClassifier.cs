namespace C4.Modules.Graph.Application.GetGraph;

public static class EnvironmentClassifier
{
    private static readonly (string Keyword, string Environment)[] EnvironmentPatterns =
    [
        ("prod", "production"),
        ("staging", "staging"),
        ("stage", "staging"),
        ("dev", "development"),
        ("test", "test"),
        ("qa", "qa"),
        ("demo", "demo"),
        ("e2e", "e2e"),
        ("trial", "trial"),
        ("sandbox", "sandbox"),
    ];

    public static string InferEnvironment(string resourceName, string? resourceGroup = null)
    {
        var lower = $"{resourceGroup} {resourceName}".ToLowerInvariant();

        foreach (var (keyword, environment) in EnvironmentPatterns)
        {
            if (ContainsSegment(lower, keyword))
                return environment;
        }

        if (lower.Contains("nonprod", StringComparison.Ordinal))
            return "nonprod";

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
