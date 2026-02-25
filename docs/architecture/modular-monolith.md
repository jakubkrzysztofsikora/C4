# Modular Monolith

## Concept

A modular monolith is a single deployable unit structured around business-aligned bounded contexts (modules). Each module is a self-contained subsystem with its own domain model, application logic, infrastructure, and API surface. Modules communicate through defined contracts, making future extraction to separate services feasible.

## Module Structure

```
src/Modules/<ModuleName>/
  <ModuleName>.Api/                  # Minimal API endpoints
    Endpoints/
    ModuleEndpoints.cs               # IEndpoint registrations
  <ModuleName>.Application/          # Use cases (vertical slices)
    <FeatureName>/
      <Feature>Command.cs
      <Feature>Handler.cs
    DomainEventHandlers/             # Reacts to domain events
    IntegrationEventHandlers/        # Reacts to cross-module events
    Ports/                           # Outbound port interfaces
      I<Name>Repository.cs
      I<Name>Service.cs
  <ModuleName>.Domain/               # Pure domain model
    <Aggregate>/
      <Aggregate>.cs
      <Aggregate>Id.cs               # Strongly typed ID
    Events/
      <DomainEvent>.cs
    Errors/
      <Module>Errors.cs
  <ModuleName>.Infrastructure/       # Adapters
    Persistence/
      <ModuleName>DbContext.cs
      Repositories/
        <Name>Repository.cs
    ExternalServices/
      <Name>Adapter.cs
    <ModuleName>InfrastructureModule.cs  # DI registration
  <ModuleName>.Tests/
    <FeatureName>/
      <Feature>HandlerTests.cs
      <Feature>EndpointTests.cs
    Domain/
      <Aggregate>Tests.cs
    Infrastructure/
      <Name>RepositoryTests.cs       # Testcontainers integration tests
```

## Cross-Module Communication

Modules do not reference each other's application or domain types. Cross-module communication happens exclusively through:

### 1. Integration Events (Async, via message broker)

```csharp
// Published by Ordering module
namespace Ordering.Application.IntegrationEvents;

public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    DateTimeOffset PlacedAt
) : IIntegrationEvent;

// Consumed by Notifications module
namespace Notifications.Application.IntegrationEventHandlers;

internal sealed class OrderPlacedHandler(INotificationService notifications)
    : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    public async Task Handle(OrderPlacedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        await notifications.SendOrderConfirmationAsync(@event.CustomerId, @event.OrderId, cancellationToken);
    }
}
```

### 2. Module API Contracts (for synchronous cross-module queries)

```csharp
// Defined in a shared contracts package (not in the module itself)
namespace Ordering.Contracts;

public interface IOrderingModule
{
    Task<OrderSummary?> GetOrderSummaryAsync(Guid orderId, CancellationToken cancellationToken);
}

// Implemented in Ordering.Infrastructure
namespace Ordering.Infrastructure;

internal sealed class OrderingModuleApi(IOrderReadRepository orders) : IOrderingModule
{
    public async Task<OrderSummary?> GetOrderSummaryAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await orders.GetSummaryAsync(orderId, cancellationToken);
    }
}
```

## Module Registration

Each module registers itself in the DI container through a single extension method:

```csharp
namespace Ordering.Infrastructure;

public static class OrderingModule
{
    public static IServiceCollection AddOrderingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly));
        services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderingDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Ordering")));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
```

## Forbidden Cross-Module Dependencies

| From | To | Verdict |
|---|---|---|
| Catalog.Application | Ordering.Domain | ❌ Never |
| Ordering.Application | Catalog.Application | ❌ Never |
| Ordering.Infrastructure | Catalog.Infrastructure | ❌ Never |
| Notifications.Application | Ordering.Contracts | ✅ Allowed |
| Host | Any Module | ✅ Allowed (composition root only) |

## Boundary Enforcement

Use ArchUnitNET to encode module boundary rules as tests:

```csharp
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void OrderingApplication_ShouldNotDependOn_CatalogApplication()
    {
        var orderingApplication = ArchRuleDefinition
            .Types().That().ResideInAssembly(Ordering.Application.AssemblyReference.Assembly);

        var catalogApplication = ArchRuleDefinition
            .Types().That().ResideInAssembly(Catalog.Application.AssemblyReference.Assembly);

        orderingApplication
            .Should().NotDependOnAny(catalogApplication)
            .Check(new Architecture(/* all assemblies */));
    }
}
```
