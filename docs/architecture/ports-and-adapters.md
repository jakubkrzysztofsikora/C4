# Ports and Adapters (Hexagonal Architecture)

## Concept

Ports and Adapters separates the core business logic (domain + application) from external concerns (databases, HTTP APIs, AI models, message brokers). The application core defines *ports* (interfaces) that describe what it needs. *Adapters* implement those ports using specific technologies.

The fundamental rule: **the dependency arrow always points inward**. Infrastructure depends on Application. Application depends on Domain. Domain depends on nothing.

## Dependency Direction

```
          ┌─────────────────────────────────────────────┐
          │                  Domain                      │
          │  (Aggregates, Value Objects, Domain Events)  │
          └───────────────────┬─────────────────────────┘
                              │ ← depends on
          ┌───────────────────▼─────────────────────────┐
          │                Application                   │
          │  (Handlers, Ports/Interfaces, Use Cases)     │
          └───────────────────┬─────────────────────────┘
                              │ ← depends on (implements ports)
          ┌───────────────────▼─────────────────────────┐
          │              Infrastructure                   │
          │  (Repositories, HTTP Clients, SK Plugins)    │
          └─────────────────────────────────────────────┘
```

## Port Definition (in Application layer)

Ports are interfaces that the application core needs. They live in `<Module>.Application/Ports/` or alongside the handler that uses them.

```csharp
namespace Ordering.Application.Ports;

public interface IOrderRepository
{
    Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IOrderFulfillmentService
{
    Task<FulfillmentResult> RequestFulfillmentAsync(OrderId orderId, CancellationToken cancellationToken);
}
```

## Adapter Implementation (in Infrastructure layer)

```csharp
namespace Ordering.Infrastructure.Persistence;

internal sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }
}
```

## Driving Adapters (Inbound)

Driving adapters call into the application. They include:

- **HTTP Endpoints** – translate HTTP requests into commands/queries
- **Message Consumers** – translate queue messages into commands
- **Background Jobs** – schedule-triggered application use cases

```csharp
namespace Ordering.Api.Endpoints;

internal sealed class PlaceOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (PlaceOrderRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToCommand(), ct);
            return result.IsSuccess
                ? Results.Created($"/orders/{result.Value.OrderId}", result.Value)
                : result.Error.ToProblemDetails();
        });
    }
}
```

## Driven Adapters (Outbound)

Driven adapters are called by the application. They include:

- **Repositories** – data persistence
- **External HTTP services** – third-party APIs
- **AI/SK plugins** – language model calls
- **Message publishers** – event bus publishing

## Fakes for Testing

For unit tests, never use mocking frameworks against ports with complex interactions. Write simple in-memory fakes:

```csharp
namespace Ordering.Tests.Fakes;

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _store = [];

    public Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }
}
```

## Rules

1. **No infrastructure type may appear in domain or application code** – not even an EF Core `DbContext` type reference in a handler
2. **Ports are defined by the application, not the infrastructure** – the shape of the interface is dictated by what the application needs
3. **Adapters are always `internal`** – they are an implementation detail registered via DI
4. **One port per concern** – do not create a god-interface that groups unrelated operations
5. **Fakes over mocks** – write simple in-memory implementations for unit test doubles; use mocks only for ports with a single method call
