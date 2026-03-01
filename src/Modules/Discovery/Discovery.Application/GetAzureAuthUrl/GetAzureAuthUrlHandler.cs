using System.Security.Cryptography;
using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.GetAzureAuthUrl;

public sealed class GetAzureAuthUrlHandler(
    IAzureIdentityService azureIdentityService,
    IOAuthStateStore oAuthStateStore)
    : IRequestHandler<GetAzureAuthUrlQuery, Result<GetAzureAuthUrlResponse>>
{
    public async Task<Result<GetAzureAuthUrlResponse>> Handle(GetAzureAuthUrlQuery request, CancellationToken cancellationToken)
    {
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await oAuthStateStore.StoreAsync(state, cancellationToken);
        var url = azureIdentityService.BuildAuthorizationUrl(request.RedirectUri, state);
        return Result<GetAzureAuthUrlResponse>.Success(new GetAzureAuthUrlResponse(url, state));
    }
}
