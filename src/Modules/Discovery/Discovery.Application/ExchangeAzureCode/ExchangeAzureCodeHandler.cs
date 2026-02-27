using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ExchangeAzureCode;

public sealed class ExchangeAzureCodeHandler(IAzureIdentityService azureIdentityService)
    : IRequestHandler<ExchangeAzureCodeCommand, Result<ExchangeAzureCodeResponse>>
{
    public async Task<Result<ExchangeAzureCodeResponse>> Handle(ExchangeAzureCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tokenResponse = await azureIdentityService.ExchangeAuthorizationCodeAsync(request.Code, request.RedirectUri, cancellationToken);
            var subscriptions = await azureIdentityService.ListSubscriptionsAsync(tokenResponse.AccessToken, cancellationToken);
            return Result<ExchangeAzureCodeResponse>.Success(new ExchangeAzureCodeResponse(subscriptions));
        }
        catch (InvalidOperationException ex)
        {
            return Result<ExchangeAzureCodeResponse>.Failure(new Error("AzureAuth.Failed", ex.Message));
        }
    }
}
