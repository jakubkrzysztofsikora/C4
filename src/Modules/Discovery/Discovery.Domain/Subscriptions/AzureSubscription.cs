using C4.Modules.Discovery.Domain.Errors;
using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.Subscriptions;

public sealed class AzureSubscription : AggregateRoot<AzureSubscriptionId>
{
    private AzureSubscription(AzureSubscriptionId id, string externalSubscriptionId, string displayName) : base(id)
    {
        ExternalSubscriptionId = externalSubscriptionId;
        DisplayName = displayName;
        ConnectedAtUtc = DateTime.UtcNow;
    }

    public string ExternalSubscriptionId { get; }

    public string DisplayName { get; }

    public DateTime ConnectedAtUtc { get; }

    public string? GitRepoUrl { get; private set; }

    public string? GitPatToken { get; private set; }

    public string? GitBranch { get; private set; }

    public string? GitRootPath { get; private set; }

    public static Result<AzureSubscription> Connect(string externalSubscriptionId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(externalSubscriptionId))
        {
            return Result<AzureSubscription>.Failure(DiscoveryErrors.InvalidSubscription(externalSubscriptionId));
        }

        return Result<AzureSubscription>.Success(
            new AzureSubscription(AzureSubscriptionId.New(), externalSubscriptionId.Trim(), displayName.Trim()));
    }

    public void ConfigureGitRepository(string? repoUrl, string? patToken, string? branch = null, string? rootPath = null)
    {
        GitRepoUrl = repoUrl?.Trim();
        GitPatToken = patToken?.Trim();
        GitBranch = branch?.Trim();
        GitRootPath = rootPath?.Trim();
    }
}
