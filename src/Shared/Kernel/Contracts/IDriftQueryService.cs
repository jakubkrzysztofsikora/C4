namespace C4.Shared.Kernel.Contracts;

public interface IDriftQueryService
{
    Task<IReadOnlyCollection<string>> GetDriftedResourceIdsAsync(IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken);
    Task<DriftRunRecord?> GetLatestRunAsync(Guid projectId, CancellationToken cancellationToken);
}
