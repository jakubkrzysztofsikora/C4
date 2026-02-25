using MediatR;
using Microsoft.Extensions.Logging;

namespace C4.Shared.Infrastructure.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling request {RequestType}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled request {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
