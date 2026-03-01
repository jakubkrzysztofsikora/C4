using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Tests.Fakes;

public sealed class AlwaysAuthorizingService : IProjectAuthorizationService
{
    public Task<Result<bool>> AuthorizeAsync(Guid projectId, CancellationToken cancellationToken)
        => Task.FromResult(Result<bool>.Success(true));

    public Task<Result<bool>> AuthorizeOwnerAsync(Guid projectId, CancellationToken cancellationToken)
        => Task.FromResult(Result<bool>.Success(true));
}
