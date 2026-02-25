# Testing Standards

## Philosophy

Tests are the primary mechanism for expressing requirements, verifying behavior, and enabling safe refactoring. Tests are first-class code and held to the same quality standards as production code. No code comments in tests. Precise, intention-revealing names only.

## Test Pyramid

```
          ▲
         /E2E\        (minimal – browser automation for critical user journeys)
        /──────\
       /Acceptance\   (per slice – WebApplicationFactory, real HTTP)
      /────────────\
     / Integration  \ (per adapter – Testcontainers, real databases)
    /────────────────\
   /    Unit Tests    \ (per handler/aggregate – in-memory fakes, fast)
  /────────────────────\
```

## Test Categories

### Unit Tests

**What:** Test a single class (handler, domain aggregate, validator) in complete isolation.

**Speed:** Milliseconds per test.

**Infrastructure:** None – all dependencies are fakes.

**Location:** `<Module>.Tests/<SliceName>/<HandlerName>Tests.cs` or `<Module>.Tests/Domain/<AggregateName>Tests.cs`

```csharp
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

    [Fact]
    public async Task Handle_CustomerNotFound_ReturnsFailure()
    {
        var command = new PlaceOrderCommand(Guid.NewGuid(), [new OrderLineDto(Guid.NewGuid(), 1)]);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CustomerNotFound);
    }
}
```

### Integration Tests

**What:** Test an adapter (repository, external service client) against real infrastructure.

**Speed:** Seconds per test (container startup is shared via `IAsyncLifetime`).

**Infrastructure:** Testcontainers (PostgreSQL, Redis, RabbitMQ, etc.)

**Location:** `<Module>.Tests/Infrastructure/<AdapterName>Tests.cs`

```csharp
public sealed class OrderRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task FindAsync_ExistingOrder_ReturnsOrder()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new OrderRepository(context);
        var order = OrderBuilder.Confirmed().Build();
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var found = await repository.FindAsync(order.Id, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be(order.Id);
    }
}
```

### Acceptance Tests

**What:** Test a complete vertical slice via HTTP, using a real (test) application host.

**Speed:** Seconds per test.

**Infrastructure:** `WebApplicationFactory` + Testcontainers for any required databases.

**Location:** `<Module>.Tests/<SliceName>/<Feature>EndpointTests.cs`

```csharp
public sealed class PlaceOrderEndpointTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task POST_Orders_ValidRequest_ReturnsCreated()
    {
        var client = fixture.CreateAuthenticatedClient();
        var request = PlaceOrderRequestBuilder.Valid().Build();

        var response = await client.PostAsJsonAsync("/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        body!.OrderId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_Orders_EmptyLines_ReturnsBadRequest()
    {
        var client = fixture.CreateAuthenticatedClient();
        var request = new PlaceOrderRequest(CustomerId: Guid.NewGuid(), Lines: []);

        var response = await client.PostAsJsonAsync("/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

## Naming Convention

**Test class:** `<SystemUnderTest>Tests`

**Test method:** `<MethodOrBehavior>_<Scenario>_<ExpectedOutcome>`

```csharp
Handle_ValidCommand_CreatesOrder
Handle_CustomerNotFound_ReturnsFailure
Place_EmptyLines_ReturnsFailure
Place_ExceedsMaxLineItems_ReturnsFailure
POST_Orders_ValidRequest_ReturnsCreated
POST_Orders_MissingAuthentication_ReturnsUnauthorized
AssessRisk_HighValueOrder_ReturnsHighRisk
```

## Test Doubles

### Prefer Fakes over Mocks

Write simple in-memory implementations for ports with multiple interactions:

```csharp
internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _store = [];

    public IReadOnlyList<Order> All => _store.Values.ToList();

    public Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }
}
```

Use mocks (NSubstitute) only for single-call verifications:

```csharp
var emailService = Substitute.For<IEmailService>();
await emailService.Received(1).SendAsync(Arg.Is<Email>(e => e.To == customer.Email));
```

### Test Builders

Use the builder pattern for complex test objects:

```csharp
internal sealed class OrderBuilder
{
    private CustomerId _customerId = CustomerId.New();
    private List<OrderLine> _lines = [OrderLine.Create(ProductId.New(), 1).Value];
    private OrderStatus _status = OrderStatus.Pending;

    public static OrderBuilder Pending() => new();
    public static OrderBuilder Confirmed() => new OrderBuilder().WithStatus(OrderStatus.Confirmed);

    public OrderBuilder WithCustomer(CustomerId id) { _customerId = id; return this; }
    public OrderBuilder WithStatus(OrderStatus status) { _status = status; return this; }

    public Order Build()
    {
        var order = Order.Place(_customerId, _lines).Value;
        if (_status == OrderStatus.Confirmed)
            order.Confirm();
        return order;
    }
}
```

## Assertion Style

Use **FluentAssertions** for all assertions. Group related assertions with `using (new AssertionScope())`:

```csharp
using (new AssertionScope())
{
    result.IsSuccess.Should().BeTrue();
    result.Value.OrderId.Should().NotBeEmpty();
}
```

Prefer specific assertion methods over generic equality:

```csharp
// ✅ Specific
result.Value.Should().HaveCount(3);
order.Status.Should().Be(OrderStatus.Confirmed);

// ❌ Generic
Assert.Equal(3, result.Value.Count);
Assert.Equal(OrderStatus.Confirmed, order.Status);
```

## Test Organization Rules

1. **One assertion group per test** – test one behavior at a time
2. **Arrange/Act/Assert separated by blank lines** – no comments labeling the sections
3. **No test helpers crossing module boundaries** – test fixtures and fakes are per-module
4. **Shared fixtures in a `Fixtures/` folder** within the test project
5. **No `Thread.Sleep` or arbitrary delays** – use `await Task.Delay` only when testing time-based behavior with a time abstraction
6. **Tests are always deterministic** – no random data unless using a fixed seed

## Testcontainers Setup

```csharp
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("ordering_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public OrderingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new OrderingDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

## Running Tests

```bash
# All tests
dotnet test

# Specific module
dotnet test src/Modules/Ordering/Ordering.Tests/

# Specific test class
dotnet test --filter "FullyQualifiedName~PlaceOrderHandlerTests"

# Specific test
dotnet test --filter "FullyQualifiedName~PlaceOrderHandlerTests.Handle_ValidCommand_CreatesOrder"

# Unit tests only (fast)
dotnet test --filter "Category!=Integration&Category!=Acceptance"
```

Tag test categories for filtering:

```csharp
[Trait("Category", "Unit")]
public sealed class PlaceOrderHandlerTests { ... }

[Trait("Category", "Integration")]
public sealed class OrderRepositoryTests { ... }

[Trait("Category", "Acceptance")]
public sealed class PlaceOrderEndpointTests { ... }
```
