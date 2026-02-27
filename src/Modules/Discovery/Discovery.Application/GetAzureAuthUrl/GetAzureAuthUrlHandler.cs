using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.GetAzureAuthUrl;

public sealed class GetAzureAuthUrlHandler(IAzureIdentityService azureIdentityService)
    : IRequestHandler<GetAzureAuthUrlQuery, Result<GetAzureAuthUrlResponse>>
{
    public Task<Result<GetAzureAuthUrlResponse>> Handle(GetAzureAuthUrlQuery request, CancellationToken cancellationToken)
    {
        var state = Guid.NewGuid().ToString("N");
        var url = azureIdentityService.BuildAuthorizationUrl(request.RedirectUri, state);
        return Task.FromResult(Result<GetAzureAuthUrlResponse>.Success(new GetAzureAuthUrlResponse(url, state)));
    }
}
