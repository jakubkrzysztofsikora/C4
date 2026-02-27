using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.GetAzureAuthUrl;

public sealed record GetAzureAuthUrlQuery(string RedirectUri) : IRequest<Result<GetAzureAuthUrlResponse>>;

public sealed record GetAzureAuthUrlResponse(string AuthUrl, string State);
