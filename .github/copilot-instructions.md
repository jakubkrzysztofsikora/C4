# GitHub Copilot Instructions

## Project Context

This repository is a **modular monolith** built with **.NET 9 + ASP.NET Core** (backend) and **React 19 + TypeScript** (frontend). It is heavily based on **Semantic Kernel** for AI orchestration. The architecture combines **Vertical Slice Architecture**, **Modular Monolith**, and **Ports and Adapters (Hexagonal Architecture)**.

## Generating Code

When generating or completing code, follow these rules:

### Architecture Rules

1. Each feature (command or query) lives in its own folder: `<Module>.Application/<FeatureName>/`
2. Handlers depend only on interfaces (ports), never on concrete infrastructure types
3. Endpoints are thin translators: HTTP request → command/query → result → HTTP response
4. No cross-module application-layer dependencies
5. Infrastructure types never appear in domain or application code

### C# Style Rules

- Use `record` for commands, queries, responses, DTOs, domain events, value objects
- Use `sealed` on all leaf classes (handlers, validators, adapters, repositories, endpoints)
- Use primary constructors
- File-scoped namespaces
- One type per file
- Always `async`/`await` – never `.Result` or `.Wait()`
- Always propagate `CancellationToken`
- Use `Result<T>` for expected failures, not exceptions
- Use strongly typed IDs (e.g., `OrderId`, `CustomerId`) for aggregate identifiers
- No `var` when the type is not obvious from the right-hand side

### TypeScript / React Style Rules

- Strict TypeScript – no `any`, use `unknown` and narrow
- Functional components with hooks only
- Co-locate feature code (component, hook, types, tests in same folder under `features/`)
- Named exports, not default exports for non-page components
- `async/await` in API calls – no `.then()` chains

### No Comments Rule

**Do not generate code comments.** Code must be self-documenting:
- Use precise, intention-revealing names
- Extract methods to name complex logic
- Use expressive domain language

The only acceptable exceptions:
- Public API XML doc comments where required for OpenAPI tooling generation
- `// TODO: #<issue-number>` referencing a real tracked issue

### Semantic Kernel Rules

- Wrap AI capabilities in `KernelPlugin` classes with `[KernelFunction]` attributes
- Register plugins via DI – never instantiate inline
- Externalize model names to configuration – never hardcode
- Use `IChatCompletionService`, never the OpenAI SDK directly
- Register `IFunctionInvocationFilter` for logging all AI calls

### Testing Rules

- Test class name: `<SystemUnderTest>Tests`
- Test method name: `<Method>_<Scenario>_<ExpectedOutcome>`
- Use FluentAssertions for all assertions
- Use in-memory fakes for ports with multiple interactions; NSubstitute for single-call verifications
- No blank assertions – always assert specific values
- Arrange/Act/Assert separated by blank lines; no comments labeling sections
- No `Thread.Sleep` – use time abstractions for time-dependent tests

## Canonical Code Examples

### Minimal API Endpoint

```csharp
namespace Ordering.Api.Endpoints;

internal sealed class PlaceOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (
            PlaceOrderRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(request.ToCommand(), cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/orders/{result.Value.OrderId}", result.Value)
                : result.Error.ToProblemDetails();
        })
        .WithTags("Orders")
        .RequireAuthorization();
    }
}
```

### Command Handler

```csharp
namespace Ordering.Application.PlaceOrder;

internal sealed class PlaceOrderHandler(
    IOrderRepository orders,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork
) : IRequestHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>
{
    public async Task<Result<PlaceOrderResponse>> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var customer = await customers.FindAsync(command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<PlaceOrderResponse>(OrderErrors.CustomerNotFound);

        var orderResult = Order.Place(customer, command.Lines.Select(l => l.ToDomain()).ToList());
        if (orderResult.IsFailure)
            return Result.Failure<PlaceOrderResponse>(orderResult.Error);

        await orders.AddAsync(orderResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PlaceOrderResponse(orderResult.Value.Id));
    }
}
```

### SK Plugin

```csharp
namespace Ordering.Infrastructure.AI;

internal sealed class OrderAnalysisPlugin(IOrderReadRepository orders)
{
    [KernelFunction, Description("Analyzes an order and returns risk assessment.")]
    public async Task<OrderRiskAssessment> AssessOrderRiskAsync(
        [Description("The order identifier.")] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orders.FindDetailAsync(orderId, cancellationToken);
        return order is null
            ? OrderRiskAssessment.OrderNotFound(orderId)
            : RiskCalculator.Assess(order);
    }
}
```

### Unit Test

```csharp
namespace Ordering.Tests.PlaceOrder;

[Trait("Category", "Unit")]
public sealed class PlaceOrderHandlerTests
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryUnitOfWork _unitOfWork = new();
    private readonly PlaceOrderHandler _sut;

    public PlaceOrderHandlerTests()
    {
        _sut = new PlaceOrderHandler(_orders, _customers, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrder()
    {
        var customer = CustomerBuilder.Active().Build();
        await _customers.AddAsync(customer, CancellationToken.None);
        var command = new PlaceOrderCommand(customer.Id.Value, [new OrderLineDto(Guid.NewGuid(), 2)]);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orders.All.Should().HaveCount(1);
    }
}
```

### React Feature Component

```typescript
// features/orders/PlaceOrderForm.tsx
interface PlaceOrderFormProps {
  onOrderPlaced: (orderId: string) => void;
}

export function PlaceOrderForm({ onOrderPlaced }: PlaceOrderFormProps) {
  const { placeOrder, isPending } = usePlaceOrder();

  async function handleSubmit(values: PlaceOrderFormValues) {
    const result = await placeOrder(values);
    if (result.orderId) {
      onOrderPlaced(result.orderId);
    }
  }

  return (
    <form onSubmit={...}>
      ...
    </form>
  );
}
```
