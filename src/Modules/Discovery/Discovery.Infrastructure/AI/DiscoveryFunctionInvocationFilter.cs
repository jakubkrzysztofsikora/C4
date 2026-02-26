using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class DiscoveryFunctionInvocationFilter(
    ILogger<DiscoveryFunctionInvocationFilter> logger,
    IDiscoveryTelemetryEventSink telemetryEventSink) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        await next(context);

        var payload = new Dictionary<string, object?>
        {
            ["plugin"] = context.Function.PluginName,
            ["function"] = context.Function.Name,
        };

        logger.LogInformation("SK function invocation {@DiscoveryFunctionInvocation}", payload);

        await telemetryEventSink.EmitAsync(
            new DiscoveryTelemetryEvent("discovery.sk.function.invoked", payload, DateTime.UtcNow),
            context.CancellationToken);
    }
}

