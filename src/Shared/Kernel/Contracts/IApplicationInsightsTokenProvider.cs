namespace C4.Shared.Kernel.Contracts;

public interface IApplicationInsightsTokenProvider
{
    Task<string?> GetAccessTokenAsync(Guid projectId, CancellationToken cancellationToken);
}
