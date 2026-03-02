using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.Errors;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Security;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.ConfigureIacRepository;

public sealed class ConfigureIacRepositoryHandler(
    IAzureSubscriptionRepository repository,
    IDataProtectionService dataProtectionService,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork)
    : IRequestHandler<ConfigureIacRepositoryCommand, Result<ConfigureIacRepositoryResponse>>
{
    public async Task<Result<ConfigureIacRepositoryResponse>> Handle(ConfigureIacRepositoryCommand request, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null)
            return Result<ConfigureIacRepositoryResponse>.Failure(DiscoveryErrors.SubscriptionNotFound());

        string? encryptedPat = request.GitPatToken;
        if (request.GitPatToken is not null)
        {
            encryptedPat = request.GitPatToken.Trim();
            encryptedPat = encryptedPat.Length == 0 ? null : dataProtectionService.Protect(encryptedPat);
        }

        if (request.GitPatToken is null)
            encryptedPat = subscription.GitPatToken;

        subscription.ConfigureGitRepository(
            request.GitRepoUrl,
            encryptedPat,
            request.GitBranch,
            request.GitRootPath);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ConfigureIacRepositoryResponse>.Success(new ConfigureIacRepositoryResponse(
            subscription.Id.Value,
            subscription.GitRepoUrl,
            subscription.GitBranch,
            subscription.GitRootPath,
            !string.IsNullOrWhiteSpace(subscription.GitPatToken)));
    }
}
