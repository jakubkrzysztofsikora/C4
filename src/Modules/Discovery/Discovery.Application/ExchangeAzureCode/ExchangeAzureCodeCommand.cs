using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.ExchangeAzureCode;

public sealed record ExchangeAzureCodeCommand(string Code, string RedirectUri, string State) : IRequest<Result<ExchangeAzureCodeResponse>>;

public sealed record ExchangeAzureCodeResponse(IReadOnlyList<AzureSubscriptionInfo> Subscriptions);
