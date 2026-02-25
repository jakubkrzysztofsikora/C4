# Agentic Patterns with Semantic Kernel

## Overview

The application uses **Semantic Kernel (SK) 1.x** as the primary AI orchestration framework. AI capabilities are implemented as composable, observable, and testable components following SK conventions.

## Core Abstractions

| SK Abstraction | Purpose | When to Use |
|---|---|---|
| `KernelPlugin` | Groups related AI functions | Always – wrap AI capabilities in plugins |
| `KernelFunction` | Single AI-callable operation | Atomic AI capability |
| `ChatCompletionAgent` | Conversational agent with instructions | Single-agent scenarios |
| `AgentGroupChat` | Multi-agent collaboration | Complex orchestration requiring specialization |
| `KernelProcess` / `KernelProcessStep` | Durable, stateful workflows | Long-running processes with checkpointing |
| `IPromptRenderFilter` | Intercept before prompt rendering | Logging, prompt modification, PII scrubbing |
| `IFunctionInvocationFilter` | Intercept function calls | Logging, retry, cost tracking |
| `IChatCompletionService` | LLM abstraction | Never call OpenAI SDK directly |

## Plugin Definition

Plugins group related `KernelFunction` methods. Each plugin class focuses on a single domain capability.

```csharp
namespace Ordering.Infrastructure.AI;

[Description("Provides order analysis capabilities.")]
internal sealed class OrderAnalysisPlugin(IOrderReadRepository orders)
{
    [KernelFunction, Description("Analyzes an order and returns risk assessment.")]
    public async Task<OrderRiskAssessment> AssessOrderRiskAsync(
        [Description("The unique identifier of the order to analyze.")] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orders.FindDetailAsync(orderId, cancellationToken);
        if (order is null)
            return OrderRiskAssessment.OrderNotFound(orderId);

        return RiskCalculator.Assess(order);
    }
}
```

## Plugin Registration

Register plugins in the module's DI composition root, not inline:

```csharp
private static IServiceCollection AddAIPlugins(this IServiceCollection services)
{
    services.AddScoped<OrderAnalysisPlugin>();
    services.AddScoped(sp =>
        KernelPluginFactory.CreateFromObject(sp.GetRequiredService<OrderAnalysisPlugin>()));
    return services;
}
```

## Single-Agent Pattern

Use `ChatCompletionAgent` for conversational agents that operate within a single context:

```csharp
internal sealed class OrderReviewAgent(Kernel kernel)
{
    private static readonly AgentDefinition Definition = new()
    {
        Instructions = """
            You are an order review specialist. When asked to review an order,
            use the available tools to assess risk and compliance. Be concise and precise.
            Always return structured JSON unless asked otherwise.
            """,
        Name = "OrderReviewAgent"
    };

    public async Task<OrderReviewResult> ReviewAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var agent = new ChatCompletionAgent
        {
            Kernel = kernel,
            Instructions = Definition.Instructions,
            Name = Definition.Name
        };

        var thread = new ChatHistoryAgentThread();
        var response = await agent.InvokeAsync(
            new ChatMessageContent(AuthorRole.User, $"Review order {orderId}"),
            thread,
            cancellationToken: cancellationToken);

        return OrderReviewResult.Parse(response.Last().Content ?? string.Empty);
    }
}
```

## Multi-Agent Pattern

Use `AgentGroupChat` when different agents have specialized capabilities that need to collaborate:

```csharp
internal sealed class OrderProcessingPipeline(Kernel kernel)
{
    public async Task<ProcessingResult> ProcessAsync(OrderId orderId, CancellationToken cancellationToken)
    {
        var reviewAgent = BuildReviewAgent(kernel);
        var complianceAgent = BuildComplianceAgent(kernel);
        var approvalAgent = BuildApprovalAgent(kernel);

        var groupChat = new AgentGroupChat(reviewAgent, complianceAgent, approvalAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                TerminationStrategy = new ApprovalTerminationStrategy()
            }
        };

        groupChat.AddChatMessage(new ChatMessageContent(
            AuthorRole.User,
            $"Process order {orderId.Value} through full review pipeline."));

        var messages = new List<ChatMessageContent>();
        await foreach (var message in groupChat.InvokeAsync(cancellationToken))
        {
            messages.Add(message);
        }

        return ProcessingResult.FromChat(messages);
    }
}
```

## Durable Process Pattern

Use `KernelProcess` for workflows that need state, checkpointing, or human-in-the-loop steps:

```csharp
internal sealed class OrderFulfillmentProcess
{
    public static KernelProcess Build()
    {
        var builder = new ProcessBuilder("OrderFulfillment");

        var validateStep = builder.AddStepFromType<ValidateOrderStep>();
        var reserveStep = builder.AddStepFromType<ReserveInventoryStep>();
        var paymentStep = builder.AddStepFromType<ProcessPaymentStep>();
        var fulfillStep = builder.AddStepFromType<FulfillOrderStep>();

        builder
            .OnInputEvent(OrderFulfillmentEvents.OrderReceived)
            .SendEventTo(new ProcessFunctionTargetBuilder(validateStep));

        validateStep
            .OnEvent(OrderFulfillmentEvents.ValidationPassed)
            .SendEventTo(new ProcessFunctionTargetBuilder(reserveStep));

        reserveStep
            .OnEvent(OrderFulfillmentEvents.InventoryReserved)
            .SendEventTo(new ProcessFunctionTargetBuilder(paymentStep));

        paymentStep
            .OnEvent(OrderFulfillmentEvents.PaymentProcessed)
            .SendEventTo(new ProcessFunctionTargetBuilder(fulfillStep));

        return builder.Build();
    }
}
```

## Observability

Register filters in the module composition root to observe all AI interactions:

```csharp
services.AddSingleton<IPromptRenderFilter, AiPromptLoggingFilter>();
services.AddSingleton<IFunctionInvocationFilter, AiFunctionLoggingFilter>();
```

```csharp
internal sealed class AiFunctionLoggingFilter(ILogger<AiFunctionLoggingFilter> logger)
    : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        logger.LogInformation(
            "AI function invoked: {Plugin}.{Function}",
            context.Function.PluginName,
            context.Function.Name);

        await next(context);

        logger.LogInformation(
            "AI function completed: {Plugin}.{Function} | Tokens: {Tokens}",
            context.Function.PluginName,
            context.Function.Name,
            context.Result.Metadata?["Usage"]);
    }
}
```

## Configuration

Never hardcode model names. Use configuration:

```csharp
services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        deploymentName: configuration["AI:DeploymentName"]!,
        endpoint: configuration["AI:Endpoint"]!,
        apiKey: configuration["AI:ApiKey"]!);
```

## Rules

1. Never call OpenAI/Azure OpenAI SDK directly – always go through `IChatCompletionService`
2. Plugins are registered via DI and injected into the Kernel; not instantiated inline
3. All prompts are in prompt template files (`.prompty` or inline string literals in a dedicated const), not scattered in handler code
4. All AI calls are logged via filters
5. Agents are deterministic by default – use `Temperature = 0` unless variation is required
6. SK plugins are unit-testable by building a `Kernel` with mocked services
