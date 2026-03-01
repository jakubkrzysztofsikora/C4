namespace C4.Shared.Kernel;

public interface IProjectAuthorizationService
{
    Task<Result<bool>> AuthorizeAsync(Guid projectId, CancellationToken cancellationToken);

    Task<Result<bool>> AuthorizeOwnerAsync(Guid projectId, CancellationToken cancellationToken);
}
