using C4.Modules.Discovery.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class DiscoveryPromptRenderFilter(
    ILogger<DiscoveryPromptRenderFilter> logger,
    IDiscoveryTelemetryEventSink telemetryEventSink) : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        await next(context);

        var payload = new Dictionary<string, object?>
        {
            ["plugin"] = context.Function?.PluginName,
            ["function"] = context.Function?.Name,
            ["promptLength"] = context.RenderedPrompt?.Length ?? 0,
        };

        logger.LogInformation("SK prompt render {@DiscoveryPromptRender}", payload);

        await telemetryEventSink.EmitAsync(
            new DiscoveryTelemetryEvent("discovery.sk.prompt.rendered", payload, DateTime.UtcNow),
            context.CancellationToken);
    }
}

