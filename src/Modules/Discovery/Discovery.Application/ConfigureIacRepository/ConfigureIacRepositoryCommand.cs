using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ConfigureIacRepository;

public sealed record ConfigureIacRepositoryCommand(
    Guid SubscriptionId,
    string? GitRepoUrl,
    string? GitPatToken,
    string? GitBranch,
    string? GitRootPath) : IRequest<Result<ConfigureIacRepositoryResponse>>;

public sealed record ConfigureIacRepositoryResponse(
    Guid SubscriptionId,
    string? GitRepoUrl,
    string? GitBranch,
    string? GitRootPath,
    bool HasGitPatToken);
