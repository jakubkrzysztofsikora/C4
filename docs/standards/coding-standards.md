# Coding Standards

## Guiding Philosophy

Code is written once and read many times. Clarity, precision, and intention-revealing design outweigh brevity. Every name is an opportunity to communicate intent. Every abstraction must earn its existence.

## No Comments Rule

**Do not write code comments.** This is a firm rule, not a preference.

Comments are a sign that the code is not self-explanatory. Fix the code, not add a comment.

### What to Do Instead

| Instead of this comment... | Do this |
|---|---|
| `// Check if user is admin` | Extract method: `IsAdmin(user)` |
| `// Magic number for timeout` | Name the constant: `DefaultTimeoutSeconds = 30` |
| `// This is needed because of a quirk in X` | Raise the abstraction level; the quirk disappears |
| `// TODO: fix later` | Create a GitHub issue immediately |

### Permitted Exceptions

1. **Public API XML docs** – only where tooling requires them (OpenAPI spec generation). Even then, keep them minimal.
2. **Temporary `// TODO:` tags** – must reference a GitHub issue number: `// TODO: #42 – remove after migration`

---

## C# Standards

### File Organization

```csharp
// File-scoped namespace – always
namespace Ordering.Application.PlaceOrder;

// One type per file – always
public sealed record PlaceOrderCommand(Guid CustomerId, IReadOnlyList<OrderLineDto> Lines);
```

### Naming

| Element | Convention | Example |
|---|---|---|
| Namespace | PascalCase, mirrors folder | `Ordering.Application.PlaceOrder` |
| Class | PascalCase | `PlaceOrderHandler` |
| Interface | `I` prefix + PascalCase | `IOrderRepository` |
| Record | PascalCase | `PlaceOrderCommand` |
| Method | PascalCase | `Handle`, `FindAsync` |
| Async method | `Async` suffix | `FindAsync`, `PlaceOrderAsync` |
| Parameter | camelCase | `orderId`, `cancellationToken` |
| Local variable | camelCase | `order`, `customer` |
| Private field | `_` prefix + camelCase | `_orders`, `_unitOfWork` |
| Constant | PascalCase | `DefaultTimeoutSeconds` |
| Enum member | PascalCase | `OrderStatus.Confirmed` |

### Type Design

Use `record` for:
- Commands and queries
- Responses and DTOs
- Value objects (with value equality)
- Domain events and integration events

Use `sealed` on all leaf classes (handlers, validators, adapters, endpoints):

```csharp
internal sealed class PlaceOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IRequestHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>
```

Use primary constructors to reduce noise:

```csharp
internal sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
```

### Async

- Always use `async`/`await`
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- Always propagate `CancellationToken`; never ignore it
- Use `ConfigureAwait(false)` in library/infrastructure code

```csharp
public async Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken)
{
    return await dbContext.Orders
        .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
        .ConfigureAwait(false);
}
```

### Error Handling

Use `Result<T>` for expected failures (validation errors, not-found, business rule violations):

```csharp
public sealed record Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public Error Error { get; }
}
```

Use exceptions only for truly exceptional, unrecoverable situations (infrastructure failures, programming errors).

Never swallow exceptions:
```csharp
// ❌ Never
try { ... } catch (Exception) { }

// ✅ Always handle or propagate
try { ... } catch (Exception ex) when (ex is not OperationCanceledException)
{
    logger.LogError(ex, "...");
    throw;
}
```

### Strongly Typed IDs

Use strongly typed IDs for all aggregate identifiers:

```csharp
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public static OrderId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
```

### LINQ

Prefer LINQ expression over imperative loops for data transformations. Keep chains readable with one operation per line for chains longer than two operations:

```csharp
var activeOrderIds = orders
    .Where(o => o.Status == OrderStatus.Active)
    .OrderByDescending(o => o.PlacedAt)
    .Select(o => o.Id)
    .ToList();
```

---

## TypeScript Standards

### TypeScript Configuration

Enforce strict TypeScript in `tsconfig.json`:

```json
{
  "compilerOptions": {
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true
  }
}
```

### No `any`

Never use `any`. Use `unknown` and narrow it:

```typescript
function parseResponse(data: unknown): OrderResponse {
  if (!isOrderResponse(data)) {
    throw new Error('Invalid order response shape');
  }
  return data;
}
```

### Naming

| Element | Convention | Example |
|---|---|---|
| Component | PascalCase | `OrderSummaryCard` |
| Hook | `use` prefix + PascalCase | `useOrderDetails` |
| Type/Interface | PascalCase | `OrderSummary`, `UseOrderDetailsResult` |
| Function | camelCase | `formatCurrency`, `parseOrderStatus` |
| Constant | SCREAMING_SNAKE_CASE | `MAX_LINE_ITEMS` |
| File | match exported name | `OrderSummaryCard.tsx` |

### Feature Co-location

Keep all files for a feature in the same folder:

```
web/src/features/orders/
  OrdersPage.tsx
  OrderSummaryCard.tsx
  useOrders.ts
  orders.types.ts
  orders.api.ts
  OrdersPage.test.tsx
  OrderSummaryCard.test.tsx
```

### Functional Patterns

Prefer functional patterns over classes for React code:

```typescript
// ✅ Functional component with hooks
function OrderSummaryCard({ orderId }: OrderSummaryCardProps) {
  const { order, isLoading } = useOrderDetails(orderId);
  if (isLoading) return <OrderSkeleton />;
  return <div>{order.total}</div>;
}

// ❌ Class component
class OrderSummaryCard extends React.Component<...>
```

### API Layer

Use typed API functions, not ad-hoc `fetch` calls in components:

```typescript
// orders.api.ts
export async function placeOrder(command: PlaceOrderCommand): Promise<PlaceOrderResponse> {
  const response = await apiClient.post<PlaceOrderResponse>('/orders', command);
  return response.data;
}
```

---

## General Rules (Both C# and TypeScript)

1. **No magic strings** – always use named constants or enums
2. **No magic numbers** – name all significant numeric literals
3. **Small methods** – if a method needs more than 20 lines, it likely has more than one responsibility
4. **No deep nesting** – maximum 2 levels of nesting; use guard clauses and early returns
5. **Fail fast** – validate at the boundary; never let invalid state propagate inward
6. **Immutability by default** – prefer `readonly`, `const`, immutable records and types
