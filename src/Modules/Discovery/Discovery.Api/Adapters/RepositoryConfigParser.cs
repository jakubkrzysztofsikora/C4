namespace C4.Modules.Discovery.Api.Adapters;

public static class RepositoryConfigParser
{
    public static IReadOnlyCollection<RepositoryConfigEntry> Parse(
        string? rawRepoConfig,
        string? defaultBranch,
        string? defaultRootPath)
    {
        if (string.IsNullOrWhiteSpace(rawRepoConfig))
            return [];

        List<RepositoryConfigEntry> entries = [];
        var rawEntries = rawRepoConfig
            .Split(['\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var rawEntry in rawEntries)
        {
            if (string.IsNullOrWhiteSpace(rawEntry))
                continue;

            if (rawEntry.StartsWith('#') || rawEntry.StartsWith("//"))
                continue;

            var parts = rawEntry.Split('|', StringSplitOptions.TrimEntries);
            var repoUrl = parts[0].Trim();
            if (repoUrl.Length == 0)
                continue;

            string? branch = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : defaultBranch;
            string? rootPath = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : defaultRootPath;

            entries.Add(new RepositoryConfigEntry(repoUrl, branch, rootPath));
        }

        return entries
            .DistinctBy(e => $"{e.RepoUrl}|{e.Branch}|{e.RootPath}", StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record RepositoryConfigEntry(string RepoUrl, string? Branch, string? RootPath);
