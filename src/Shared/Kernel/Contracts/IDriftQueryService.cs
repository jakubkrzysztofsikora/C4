namespace C4.Shared.Kernel.Contracts;

public interface IDriftQueryService
{
    Task<IReadOnlyCollection<string>> GetDriftedResourceIdsAsync(IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken);
}
