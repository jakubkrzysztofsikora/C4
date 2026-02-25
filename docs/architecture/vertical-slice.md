# Vertical Slice Architecture

## Concept

A vertical slice is a self-contained implementation of a single feature or use case that spans all relevant layers – from the HTTP endpoint down to the database. Instead of organizing code by technical layer (Controllers, Services, Repositories), code is organized by feature.

## Folder Structure

```
src/Modules/Ordering/Ordering.Application/
  PlaceOrder/
    PlaceOrderCommand.cs
    PlaceOrderHandler.cs
    PlaceOrderValidator.cs
    PlaceOrderResponse.cs
  GetOrderById/
    GetOrderByIdQuery.cs
    GetOrderByIdHandler.cs
    GetOrderByIdResponse.cs
  CancelOrder/
    CancelOrderCommand.cs
    CancelOrderHandler.cs

src/Modules/Ordering/Ordering.Api/
  Endpoints/
    PlaceOrderEndpoint.cs
    GetOrderByIdEndpoint.cs
    CancelOrderEndpoint.cs

src/Modules/Ordering/Ordering.Tests/
  PlaceOrder/
    PlaceOrderHandlerTests.cs
    PlaceOrderEndpointTests.cs
  GetOrderById/
    GetOrderByIdHandlerTests.cs
```

## Canonical Slice: Command (Write Operation)

### Command Record

```csharp
namespace Ordering.Application.PlaceOrder;

public sealed record PlaceOrderCommand(
    Guid CustomerId,
    IReadOnlyList<OrderLineDto> Lines
) : IRequest<Result<PlaceOrderResponse>>;
```

### Handler

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

### Validator

```csharp
namespace Ordering.Application.PlaceOrder;

internal sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new OrderLineDtoValidator());
    }
}
```

### Response Record

```csharp
namespace Ordering.Application.PlaceOrder;

public sealed record PlaceOrderResponse(Guid OrderId);
```

### Endpoint

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

## Canonical Slice: Query (Read Operation)

Queries return data directly; they do not go through the domain model. They may use lightweight read models.

```csharp
namespace Ordering.Application.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDetailDto>>;

internal sealed class GetOrderByIdHandler(IOrderReadRepository orders)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDetailDto>>
{
    public async Task<Result<OrderDetailDto>> Handle(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        var order = await orders.FindDetailAsync(query.OrderId, cancellationToken);
        return order is null
            ? Result.Failure<OrderDetailDto>(OrderErrors.NotFound)
            : Result.Success(order);
    }
}
```

## Rules

1. A slice folder owns its `Command`/`Query`, `Handler`, `Validator`, `Response`, and feature-specific `Error` constants
2. Handlers depend only on ports (interfaces); never on concrete infrastructure types
3. No shared application services between slices – if two slices share logic, extract it to the domain layer
4. Each slice has at least one unit test for the handler and one acceptance test for the endpoint
5. Slice code is `internal` by default; only commands, queries, and responses are `public`
