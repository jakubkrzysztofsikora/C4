using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ExchangeAzureCode;

public sealed class ExchangeAzureCodeHandler(
    IAzureIdentityService azureIdentityService,
    IAzureTokenStore tokenStore,
    IOAuthStateStore oAuthStateStore)
    : IRequestHandler<ExchangeAzureCodeCommand, Result<ExchangeAzureCodeResponse>>
{
    private static readonly Error InvalidOAuthState = new("AzureAuth.InvalidState", "OAuth state parameter is invalid or expired.");

    public async Task<Result<ExchangeAzureCodeResponse>> Handle(ExchangeAzureCodeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.State))
            return Result<ExchangeAzureCodeResponse>.Failure(InvalidOAuthState);

        bool stateValid = await oAuthStateStore.ValidateAndConsumeAsync(request.State, cancellationToken);
        if (!stateValid)
            return Result<ExchangeAzureCodeResponse>.Failure(InvalidOAuthState);

        try
        {
            AzureTokenResponse tokenResponse = await azureIdentityService.ExchangeAuthorizationCodeAsync(request.Code, request.RedirectUri, cancellationToken);
            IReadOnlyList<AzureSubscriptionInfo> subscriptions = await azureIdentityService.ListSubscriptionsAsync(tokenResponse.AccessToken, cancellationToken);

            AzureTokenInfo tokenInfo = new(
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

            foreach (AzureSubscriptionInfo subscription in subscriptions)
            {
                await tokenStore.StoreAsync(subscription.SubscriptionId, tokenInfo, cancellationToken);
            }

            return Result<ExchangeAzureCodeResponse>.Success(new ExchangeAzureCodeResponse(subscriptions));
        }
        catch (InvalidOperationException ex)
        {
            return Result<ExchangeAzureCodeResponse>.Failure(new Error("AzureAuth.Failed", ex.Message));
        }
    }
}
