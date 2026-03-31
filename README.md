# Philiprehberger.EventBus

[![CI](https://github.com/philiprehberger/dotnet-event-bus/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-event-bus/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.EventBus.svg)](https://www.nuget.org/packages/Philiprehberger.EventBus)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-event-bus)](https://github.com/philiprehberger/dotnet-event-bus/commits/main)

In-process publish/subscribe event bus with priority ordering, handler filtering, timeout enforcement, and Microsoft DI integration.

## Installation

```bash
dotnet add package Philiprehberger.EventBus
```

## Usage

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

using var subscription = bus.Subscribe<OrderPlaced>(async (e, ct) =>
{
    Console.WriteLine($"Order {e.OrderId} placed");
});

await bus.PublishAsync(new OrderPlaced(OrderId: 42));

record OrderPlaced(int OrderId);
```

### Publish and Subscribe

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

// Subscribe returns an IDisposable — dispose it to unsubscribe
using var sub = bus.Subscribe<UserRegistered>(async (e, ct) =>
{
    await SendWelcomeEmailAsync(e.Email, ct);
});

// Publish fires all handlers concurrently
await bus.PublishAsync(new UserRegistered("user@example.com"));

record UserRegistered(string Email);
```

### Handler Priority

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

// Lower priority number executes first
bus.Subscribe<OrderPlaced>((e, ct) =>
{
    Console.WriteLine("Validate order");
    return Task.CompletedTask;
}, priority: 10);

bus.Subscribe<OrderPlaced>((e, ct) =>
{
    Console.WriteLine("Send confirmation email");
    return Task.CompletedTask;
}, priority: 20);

await bus.PublishAsync(new OrderPlaced(1));
// Output: Validate order, then Send confirmation email

record OrderPlaced(int OrderId);
```

### Handler Filtering

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

// Only handle high-value orders
bus.Subscribe<OrderPlaced>(
    (e, ct) =>
    {
        Console.WriteLine($"High-value order: {e.OrderId}");
        return Task.CompletedTask;
    },
    filter: e => e.Total > 1000);

await bus.PublishAsync(new OrderPlaced(1, 500));   // Skipped
await bus.PublishAsync(new OrderPlaced(2, 2000));  // Handled

record OrderPlaced(int OrderId, decimal Total);
```

### Error Handling

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus(new EventBusOptions
{
    ThrowOnHandlerError = false,
    OnHandlerError = ex => Console.Error.WriteLine($"Handler failed: {ex.Message}")
});

bus.Subscribe<OrderPlaced>((_, _) => throw new InvalidOperationException("oops"));

await bus.PublishAsync(new OrderPlaced(1));
// Logs "Handler failed: oops" without propagating the exception

record OrderPlaced(int OrderId);
```

### Handler Timeout

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus(new EventBusOptions
{
    ThrowOnHandlerError = true,
    HandlerTimeout = TimeSpan.FromSeconds(5)
});

bus.Subscribe<OrderPlaced>(async (e, ct) =>
{
    await ProcessOrderAsync(e.OrderId, ct);
});

// Throws TimeoutException if the handler exceeds 5 seconds
await bus.PublishAsync(new OrderPlaced(1));

record OrderPlaced(int OrderId);
```

### DI Registration

```csharp
using Philiprehberger.EventBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventBus(options =>
{
    options.ThrowOnHandlerError = true;
    options.MaxConcurrency = 4;
    options.HandlerTimeout = TimeSpan.FromSeconds(10);
    options.OnHandlerError = ex => Console.Error.WriteLine(ex);
});

var app = builder.Build();
```

### Handler Classes

```csharp
using Philiprehberger.EventBus;

public record OrderShipped(int OrderId, string TrackingNumber);

public class OrderShippedHandler : IEventHandler<OrderShipped>
{
    public async Task HandleAsync(OrderShipped @event, CancellationToken ct)
    {
        await NotifyCustomerAsync(@event.OrderId, @event.TrackingNumber, ct);
    }
}
```

## API

### `IEventBus`

| Method | Description |
|--------|-------------|
| `PublishAsync<T>(@event, ct)` | Publishes an event to all registered handlers for the type |
| `Subscribe<T>(handler, priority, filter)` | Subscribes a handler function; returns `IDisposable` to unsubscribe |

### `IEventHandler<T>`

| Method | Description |
|--------|-------------|
| `HandleAsync(@event, ct)` | Handles an event of type `T` asynchronously |

### `EventBusOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ThrowOnHandlerError` | `bool` | `false` | Propagate handler exceptions to the publisher |
| `MaxConcurrency` | `int` | `0` | Max concurrent handler invocations (0 = unlimited) |
| `OnHandlerError` | `Action<Exception>?` | `null` | Callback invoked when any handler throws an exception |
| `HandlerTimeout` | `TimeSpan?` | `null` | Timeout per handler invocation; throws `TimeoutException` if exceeded |

### `Subscribe<T>` Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `handler` | `Func<T, CancellationToken, Task>` | required | The handler function to invoke |
| `priority` | `int` | `0` | Execution priority; lower values execute first |
| `filter` | `Func<T, bool>?` | `null` | Predicate evaluated before invoking; handler is skipped if it returns `false` |

### `ServiceCollectionExtensions`

| Method | Description |
|--------|-------------|
| `AddEventBus(configure?)` | Registers `IEventBus` as singleton and scans for `IEventHandler<T>` implementations |

## Development

```bash
dotnet build src/Philiprehberger.EventBus.csproj --configuration Release
```

## Support

If you find this project useful:

⭐ [Star the repo](https://github.com/philiprehberger/dotnet-event-bus)

🐛 [Report issues](https://github.com/philiprehberger/dotnet-event-bus/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

💡 [Suggest features](https://github.com/philiprehberger/dotnet-event-bus/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)

❤️ [Sponsor development](https://github.com/sponsors/philiprehberger)

🌐 [All Open Source Projects](https://philiprehberger.com/open-source-packages)

💻 [GitHub Profile](https://github.com/philiprehberger)

🔗 [LinkedIn Profile](https://www.linkedin.com/in/philiprehberger)

## License

[MIT](LICENSE)
