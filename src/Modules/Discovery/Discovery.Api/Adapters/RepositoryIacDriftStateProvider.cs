using System.Diagnostics;
using System.Text.RegularExpressions;
using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed partial class RepositoryIacDriftStateProvider(
    IServiceScopeFactory scopeFactory,
    IIacStateParser iacStateParser,
    IDataProtectionService dataProtectionService) : IIacRepositoryStateProvider
{
    public async Task<IReadOnlyCollection<IacResourceRecord>> CollectAsync(
        Guid subscriptionId,
        string? environment,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RepositoryConfigEntry> repositoryEntries;
        string? gitPatToken;

        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            IAzureSubscriptionRepository repository = scope.ServiceProvider.GetRequiredService<IAzureSubscriptionRepository>();
            var subscription = await repository.GetByIdAsync(subscriptionId, cancellationToken);

            if (subscription is null)
                return [];

            repositoryEntries = RepositoryConfigParser.Parse(
                subscription.GitRepoUrl,
                subscription.GitBranch,
                subscription.GitRootPath);
            if (repositoryEntries.Count == 0)
                return [];

            gitPatToken = string.IsNullOrWhiteSpace(subscription.GitPatToken)
                ? null
                : dataProtectionService.Unprotect(subscription.GitPatToken);
        }

        var normalizedEnvironment = NormalizeEnvironment(environment);
        Dictionary<string, IacResourceRecord> collected = new(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in repositoryEntries)
        {
            ValidateGitUrl(entry.RepoUrl);
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"c4-drift-iac-{Guid.NewGuid():N}");
            try
            {
                string cloneUrl = BuildAuthenticatedCloneUrl(entry.RepoUrl, gitPatToken);
                await CloneRepositoryAsync(cloneUrl, tempDirectory, entry.Branch, cancellationToken);

                string scanRoot = ResolveSearchRoot(tempDirectory, entry.RootPath);
                var hasEnvironmentFolder = Directory.Exists(Path.Combine(scanRoot, "environments"));
                var environmentRoot = hasEnvironmentFolder
                    ? Path.Combine(scanRoot, "environments")
                    : scanRoot;

                foreach (var filePath in EnumerateBicepEntryFiles(environmentRoot, normalizedEnvironment))
                {
                    var parsed = await ParseBicepFileRecursiveAsync(
                        filePath,
                        parameterOverrides: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                        fileStack: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                        cancellationToken);
                    foreach (var record in parsed)
                    {
                        collected[record.ResourceId] = record;
                    }
                }

                foreach (var terraformFile in EnumerateTerraformFiles(environmentRoot, normalizedEnvironment))
                {
                    var content = await File.ReadAllTextAsync(terraformFile, cancellationToken);
                    var parsed = await iacStateParser.ParseAsync(content, "terraform", cancellationToken);
                    foreach (var record in parsed)
                    {
                        collected[record.ResourceId] = record;
                    }
                }
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        return collected.Values.ToArray();
    }

    private async Task<IReadOnlyCollection<IacResourceRecord>> ParseBicepFileRecursiveAsync(
        string filePath,
        IReadOnlyDictionary<string, string> parameterOverrides,
        HashSet<string> fileStack,
        CancellationToken cancellationToken)
    {
        string normalizedFilePath = Path.GetFullPath(filePath);
        if (!File.Exists(normalizedFilePath))
            return [];

        if (!fileStack.Add(normalizedFilePath))
            return [];

        try
        {
            var content = await File.ReadAllTextAsync(normalizedFilePath, cancellationToken);
            var lines = content.Split('\n');

            Dictionary<string, string> context = new(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                var match = ParamDeclarationRegex().Match(line);
                if (!match.Success)
                    continue;

                var name = match.Groups["name"].Value;
                var rawDefault = match.Groups["value"].Success ? match.Groups["value"].Value : null;
                var evaluatedDefault = EvaluateExpression(rawDefault, context);
                if (!string.IsNullOrWhiteSpace(evaluatedDefault))
                {
                    context[name] = evaluatedDefault;
                }
            }

            foreach (var (key, value) in parameterOverrides)
            {
                context[key] = value;
            }

            foreach (var line in lines)
            {
                var match = VariableDeclarationRegex().Match(line);
                if (!match.Success)
                    continue;

                var name = match.Groups["name"].Value;
                var rawValue = match.Groups["value"].Value;
                var evaluatedValue = EvaluateExpression(rawValue, context);
                if (!string.IsNullOrWhiteSpace(evaluatedValue))
                {
                    context[name] = evaluatedValue;
                }
            }

            List<IacResourceRecord> records = [];

            for (var i = 0; i < lines.Length; i++)
            {
                var declarationLine = lines[i];
                var resourceMatch = ResourceDeclarationRegex().Match(declarationLine);
                if (resourceMatch.Success)
                {
                    var symbol = resourceMatch.Groups["symbol"].Value;
                    var rawType = resourceMatch.Groups["type"].Value;
                    var armType = rawType.Split('@', 2)[0].Trim();
                    if (armType.Length == 0)
                        armType = "bicep/resource";

                    var block = ReadObjectBlock(lines, i, out var consumedLines);
                    i += consumedLines - 1;

                    var name = symbol;
                    foreach (var blockLine in block)
                    {
                        var nameMatch = NameAssignmentRegex().Match(blockLine);
                        if (!nameMatch.Success)
                            continue;

                        var evaluated = EvaluateExpression(nameMatch.Groups["value"].Value, context);
                        if (!string.IsNullOrWhiteSpace(evaluated))
                        {
                            name = evaluated;
                            break;
                        }
                    }

                    var resourceId = $"/providers/{armType}/{name}".ToLowerInvariant();
                    records.Add(new IacResourceRecord(resourceId, armType, name));
                    continue;
                }

                var moduleMatch = ModuleDeclarationRegex().Match(declarationLine);
                if (!moduleMatch.Success)
                    continue;

                var relativePath = moduleMatch.Groups["path"].Value.Trim();
                if (relativePath.Length == 0)
                    continue;

                var moduleFullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(normalizedFilePath)!, relativePath));
                if (!File.Exists(moduleFullPath))
                    continue;

                var moduleBlock = ReadObjectBlock(lines, i, out var moduleConsumedLines);
                i += moduleConsumedLines - 1;

                var moduleOverrides = ParseModuleParameterOverrides(moduleBlock, context);
                var nested = await ParseBicepFileRecursiveAsync(
                    moduleFullPath,
                    moduleOverrides,
                    fileStack,
                    cancellationToken);
                records.AddRange(nested);
            }

            return records
                .Where(record => !string.IsNullOrWhiteSpace(record.ResourceId))
                .GroupBy(record => record.ResourceId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
        }
        finally
        {
            fileStack.Remove(normalizedFilePath);
        }
    }

    private static IReadOnlyDictionary<string, string> ParseModuleParameterOverrides(
        IReadOnlyList<string> moduleBlock,
        IReadOnlyDictionary<string, string> context)
    {
        var joined = string.Join("\n", moduleBlock);
        var paramsMatch = ModuleParamsBlockRegex().Match(joined);
        if (!paramsMatch.Success)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        var paramsText = paramsMatch.Groups["params"].Value;
        var lines = paramsText.Split('\n');

        foreach (var line in lines)
        {
            var match = ModuleParamLineRegex().Match(line);
            if (!match.Success)
                continue;

            var key = match.Groups["key"].Value;
            var value = EvaluateExpression(match.Groups["value"].Value, context);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static List<string> ReadObjectBlock(string[] lines, int startIndex, out int consumedLines)
    {
        List<string> block = [];
        int depth = 0;
        var started = false;
        consumedLines = 0;

        for (var i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            block.Add(line);
            consumedLines++;

            foreach (var ch in line)
            {
                if (ch == '{')
                {
                    depth++;
                    started = true;
                }
                else if (ch == '}')
                {
                    depth--;
                }
            }

            if (started && depth <= 0)
                break;
        }

        return block;
    }

    private static IEnumerable<string> EnumerateBicepEntryFiles(string rootDirectory, string? environment)
    {
        if (!Directory.Exists(rootDirectory))
            return [];

        var allFiles = Directory
            .EnumerateFiles(rootDirectory, "*.bicep", SearchOption.AllDirectories)
            .ToArray();
        if (allFiles.Length == 0)
            return [];

        IReadOnlyCollection<string> environmentTokens = ExpandEnvironmentAliases(environment);
        var scopedFiles = environmentTokens.Count == 0
            ? allFiles
            : allFiles.Where(file => MatchesEnvironment(file, environmentTokens)).ToArray();

        if (scopedFiles.Length == 0)
            return [];

        var mainFiles = scopedFiles
            .Where(file => Path.GetFileName(file).Equals("main.bicep", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (mainFiles.Length > 0)
            return mainFiles;

        return scopedFiles;
    }

    private static IEnumerable<string> EnumerateTerraformFiles(string rootDirectory, string? environment)
    {
        if (!Directory.Exists(rootDirectory))
            return [];

        var allFiles = Directory
            .EnumerateFiles(rootDirectory, "*.tf", SearchOption.AllDirectories)
            .ToArray();
        if (allFiles.Length == 0)
            return [];

        IReadOnlyCollection<string> environmentTokens = ExpandEnvironmentAliases(environment);
        var scopedFiles = environmentTokens.Count == 0
            ? allFiles
            : allFiles.Where(file => MatchesEnvironment(file, environmentTokens)).ToArray();

        if (scopedFiles.Length == 0)
            return [];

        var mainFiles = scopedFiles
            .Where(file => Path.GetFileName(file).Equals("main.tf", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (mainFiles.Length > 0)
            return mainFiles;

        return scopedFiles;
    }

    private static string? NormalizeEnvironment(string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            return null;

        var normalized = environment.Trim();
        if (normalized.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;

        return normalized;
    }

    private static IReadOnlyCollection<string> ExpandEnvironmentAliases(string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            return [];

        var token = environment.Trim().ToLowerInvariant();
        HashSet<string> aliases = [token];
        switch (token)
        {
            case "prod":
            case "prd":
            case "production":
                aliases.Add("prod");
                aliases.Add("prd");
                aliases.Add("production");
                break;
            case "stage":
            case "staging":
            case "stg":
                aliases.Add("stage");
                aliases.Add("staging");
                aliases.Add("stg");
                break;
            case "dev":
            case "development":
                aliases.Add("dev");
                aliases.Add("development");
                break;
            case "test":
            case "tst":
            case "qa":
                aliases.Add("test");
                aliases.Add("tst");
                aliases.Add("qa");
                break;
        }

        return aliases.ToArray();
    }

    private static bool MatchesEnvironment(string filePath, IReadOnlyCollection<string> environmentTokens)
    {
        if (environmentTokens.Count == 0)
            return true;

        var normalized = filePath.Replace('\\', '/').ToLowerInvariant();
        var segments = normalized.Split(['/', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Any(segment => environmentTokens.Contains(segment));
    }

    private static string? EvaluateExpression(string? rawValue, IReadOnlyDictionary<string, string> context)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var value = RemoveInlineComment(rawValue).Trim().TrimEnd(',');
        if (value.Length == 0)
            return null;

        if (value.StartsWith("toLower(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            var inner = value["toLower(".Length..^1];
            var evaluatedInner = EvaluateExpression(inner, context);
            return evaluatedInner?.ToLowerInvariant();
        }

        if (value.StartsWith("toUpper(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            var inner = value["toUpper(".Length..^1];
            var evaluatedInner = EvaluateExpression(inner, context);
            return evaluatedInner?.ToUpperInvariant();
        }

        if (value.Contains('+'))
        {
            var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var evaluatedParts = parts
                .Select(part => EvaluateExpression(part, context))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            if (evaluatedParts.Length > 0)
                return string.Concat(evaluatedParts);
        }

        if (TryExtractQuotedValue(value, out var quoted))
        {
            return InterpolateTemplate(quoted, context);
        }

        if (context.TryGetValue(value, out var mapped))
            return mapped;

        return null;
    }

    private static string RemoveInlineComment(string value)
    {
        var index = value.IndexOf("//", StringComparison.Ordinal);
        return index >= 0 ? value[..index] : value;
    }

    private static bool TryExtractQuotedValue(string value, out string extracted)
    {
        extracted = string.Empty;
        if (value.Length < 2)
            return false;

        if ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"'))
        {
            extracted = value[1..^1];
            return true;
        }

        return false;
    }

    private static string InterpolateTemplate(string template, IReadOnlyDictionary<string, string> context)
    {
        return InterpolationRegex().Replace(template, match =>
        {
            var key = match.Groups["name"].Value;
            return context.TryGetValue(key, out var value) ? value : key;
        });
    }

    private static void ValidateGitUrl(string repoUrl)
    {
        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Git repository URL is not a valid URI.");

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Only HTTPS Git repository URLs are allowed.");

        if (repoUrl.StartsWith('-'))
            throw new ArgumentException("Git repository URL must not start with a dash.");

        if (ShellMetacharsPattern().IsMatch(repoUrl))
            throw new ArgumentException("Git repository URL contains disallowed characters.");
    }

    private static string BuildAuthenticatedCloneUrl(string repoUrl, string? patToken)
    {
        if (string.IsNullOrWhiteSpace(patToken))
            return repoUrl;

        Uri uri = new(repoUrl);
        var encodedPat = Uri.EscapeDataString(patToken);
        return $"{uri.Scheme}://pat:{encodedPat}@{uri.Host}{uri.PathAndQuery}";
    }

    private static async Task CloneRepositoryAsync(string cloneUrl, string targetDirectory, string? branch, CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "git",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("clone");
        startInfo.ArgumentList.Add("--depth");
        startInfo.ArgumentList.Add("1");
        if (!string.IsNullOrWhiteSpace(branch))
        {
            startInfo.ArgumentList.Add("--branch");
            startInfo.ArgumentList.Add(branch.Trim());
        }

        startInfo.ArgumentList.Add(cloneUrl);
        startInfo.ArgumentList.Add(targetDirectory);

        using Process process = new() { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            string errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"git clone failed with exit code {process.ExitCode}: {errorOutput}");
        }
    }

    private static string ResolveSearchRoot(string repositoryDirectory, string? configuredRoot)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
            return repositoryDirectory;

        string normalized = configuredRoot.Trim().Trim('/').Trim('\\');
        if (normalized.Length == 0)
            return repositoryDirectory;

        string fullPath = Path.GetFullPath(Path.Combine(repositoryDirectory, normalized));
        string repoRoot = Path.GetFullPath(repositoryDirectory);
        if (!fullPath.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(fullPath))
            return repositoryDirectory;

        return fullPath;
    }

    [GeneratedRegex(@"^\s*param\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s+[^\r\n=]+(?:=\s*(?<value>.+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ParamDeclarationRegex();

    [GeneratedRegex(@"^\s*var\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex VariableDeclarationRegex();

    [GeneratedRegex(@"^\s*resource\s+(?<symbol>[A-Za-z_][A-Za-z0-9_]*)\s+'(?<type>[^']+)'", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ResourceDeclarationRegex();

    [GeneratedRegex(@"^\s*module\s+(?<symbol>[A-Za-z_][A-Za-z0-9_]*)\s+'(?<path>[^']+\.bicep)'", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ModuleDeclarationRegex();

    [GeneratedRegex(@"^\s*name\s*:\s*(?<value>.+?)(,)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex NameAssignmentRegex();

    [GeneratedRegex(@"params\s*:\s*{(?<params>[\s\S]*?)}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ModuleParamsBlockRegex();

    [GeneratedRegex(@"^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<value>.+?)(,)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ModuleParamLineRegex();

    [GeneratedRegex(@"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled)]
    private static partial Regex InterpolationRegex();

    [GeneratedRegex(@"[;|&`$(){}\[\]<>!]", RegexOptions.Compiled)]
    private static partial Regex ShellMetacharsPattern();
}
