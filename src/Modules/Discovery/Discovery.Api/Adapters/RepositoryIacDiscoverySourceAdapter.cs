using System.Diagnostics;
using System.Text.RegularExpressions;
using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed partial class RepositoryIacDiscoverySourceAdapter(
    IServiceScopeFactory scopeFactory,
    IIacStateParser iacStateParser) : IDiscoverySourceAdapter
{
    public DiscoverySourceKind Source => DiscoverySourceKind.RepositoryIac;

    public async Task<IReadOnlyCollection<DiscoveryResourceDescriptor>> GetResourcesAsync(
        NormalizedDiscoveryRequest request,
        CancellationToken cancellationToken)
    {
        string? gitRepoUrl;
        string? gitPatToken;

        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            IAzureSubscriptionRepository repository = scope.ServiceProvider.GetRequiredService<IAzureSubscriptionRepository>();
            var subscription = await repository.GetFirstAsync(cancellationToken);

            if (subscription is null || string.IsNullOrWhiteSpace(subscription.GitRepoUrl))
            {
                return Array.Empty<DiscoveryResourceDescriptor>();
            }

            gitRepoUrl = subscription.GitRepoUrl;
            gitPatToken = subscription.GitPatToken;
        }

        ValidateGitUrl(gitRepoUrl);

        string tempDirectory = Path.Combine(Path.GetTempPath(), $"c4-iac-{Guid.NewGuid():N}");
        try
        {
            string cloneUrl = BuildAuthenticatedCloneUrl(gitRepoUrl, gitPatToken);
            await CloneRepositoryAsync(cloneUrl, tempDirectory, cancellationToken);

            IEnumerable<string> iacFiles = EnumerateIacFiles(tempDirectory);
            List<DiscoveryResourceDescriptor> descriptors = [];

            foreach (string filePath in iacFiles)
            {
                string format = ResolveFormat(filePath);
                string content = await File.ReadAllTextAsync(filePath, cancellationToken);
                IReadOnlyCollection<IacResourceRecord> records = await iacStateParser.ParseAsync(content, format, cancellationToken);

                foreach (IacResourceRecord record in records)
                {
                    descriptors.Add(new DiscoveryResourceDescriptor(
                        record.ResourceId,
                        record.ResourceType,
                        record.Name,
                        null,
                        Source));
                }
            }

            return descriptors;
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
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
        return $"{uri.Scheme}://pat:{patToken}@{uri.Host}{uri.PathAndQuery}";
    }

    private static async Task CloneRepositoryAsync(string cloneUrl, string targetDirectory, CancellationToken cancellationToken)
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
