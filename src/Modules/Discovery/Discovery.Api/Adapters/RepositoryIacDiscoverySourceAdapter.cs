using System.Diagnostics;
using System.Text.RegularExpressions;
using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed partial class RepositoryIacDiscoverySourceAdapter(
    IServiceScopeFactory scopeFactory,
    IIacStateParser iacStateParser,
    IDataProtectionService dataProtectionService) : IDiscoverySourceAdapter
{
    public DiscoverySourceKind Source => DiscoverySourceKind.RepositoryIac;

    public async Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(
        NormalizedDiscoveryRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RepositoryConfigEntry> repositoryEntries;
        string? gitPatToken;

        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            IAzureSubscriptionRepository repository = scope.ServiceProvider.GetRequiredService<IAzureSubscriptionRepository>();
            var subscription = await repository.GetFirstAsync(cancellationToken);

            if (subscription is null)
            {
                return Array.Empty<DiscoveryResourceDescriptor>();
            }

            repositoryEntries = RepositoryConfigParser.Parse(
                subscription.GitRepoUrl,
                subscription.GitBranch,
                subscription.GitRootPath);
            if (repositoryEntries.Count == 0)
            {
                return Array.Empty<DiscoveryResourceDescriptor>();
            }

            gitPatToken = string.IsNullOrWhiteSpace(subscription.GitPatToken)
                ? null
                : dataProtectionService.Unprotect(subscription.GitPatToken);
        }

        Dictionary<string, DiscoveryResourceDescriptor> resources = new(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in repositoryEntries)
        {
            ValidateGitUrl(entry.RepoUrl);
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"c4-iac-{Guid.NewGuid():N}");
            try
            {
                string cloneUrl = BuildAuthenticatedCloneUrl(entry.RepoUrl, gitPatToken);
                await CloneRepositoryAsync(cloneUrl, tempDirectory, entry.Branch, cancellationToken);

                string searchRoot = ResolveSearchRoot(tempDirectory, entry.RootPath);
                IEnumerable<string> iacFiles = EnumerateIacFiles(searchRoot);

                foreach (string filePath in iacFiles)
                {
                    string format = ResolveFormat(filePath);
                    string content = await File.ReadAllTextAsync(filePath, cancellationToken);
                    IReadOnlyCollection<IacResourceRecord> records = await iacStateParser.ParseAsync(content, format, cancellationToken);

                    foreach (IacResourceRecord record in records)
                    {
                        resources[record.ResourceId] = new DiscoveryResourceDescriptor(
                            record.ResourceId,
                            record.ResourceType,
                            record.Name,
                            null,
                            Source);
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

        return resources.Values.ToArray();
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
        {
            return repoUrl;
        }

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

    private static IEnumerable<string> EnumerateIacFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*.bicep", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(directory, "*.tf", SearchOption.AllDirectories));

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

    private static string ResolveFormat(string filePath) =>
        Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant() switch
        {
            "bicep" => "bicep",
            "tf" => "terraform",
            _ => string.Empty
        };

    [GeneratedRegex(@"[;|&`$(){}\[\]<>!]")]
    private static partial Regex ShellMetacharsPattern();
}
