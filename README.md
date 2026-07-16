# Philiprehberger.EventBus

[![CI](https://github.com/philiprehberger/dotnet-event-bus/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-event-bus/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.EventBus.svg)](https://www.nuget.org/packages/Philiprehberger.EventBus)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-event-bus)](https://github.com/philiprehberger/dotnet-event-bus/commits/main)

![Philiprehberger.EventBus](https://raw.githubusercontent.com/philiprehberger/dotnet-event-bus/main/package-card.webp)

In-process publish/subscribe event bus with middleware pipeline, dead-letter queue, event replay, and Microsoft DI integration.

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

### Sequential Dispatch

```csharp
using Philiprehberger.EventBus;

// By default handlers run concurrently. Enable SequentialDispatch to await each
// handler in strict priority order before starting the next — useful for ordered
// pipelines (validate → mutate → notify).
var bus = new EventBus(new EventBusOptions { SequentialDispatch = true });

bus.Subscribe<OrderPlaced>((e, _) => { Console.WriteLine("1. Validate"); return Task.CompletedTask; }, priority: 10);
bus.Subscribe<OrderPlaced>((e, _) => { Console.WriteLine("2. Charge"); return Task.CompletedTask; }, priority: 20);
bus.Subscribe<OrderPlaced>((e, _) => { Console.WriteLine("3. Notify"); return Task.CompletedTask; }, priority: 30);

await bus.PublishAsync(new OrderPlaced(1));
// Output (each completes before the next begins): 1. Validate, 2. Charge, 3. Notify

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

### Dead-Letter Queue

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus(new EventBusOptions
{
    ThrowOnHandlerError = false,
    OnDeadLetter = (evt, ex) =>
        Console.Error.WriteLine($"Dead letter: {evt.GetType().Name} failed with {ex.Message}")
});

bus.Subscribe<OrderPlaced>((_, _) => throw new InvalidOperationException("payment failed"));

await bus.PublishAsync(new OrderPlaced(1));
// Logs "Dead letter: OrderPlaced failed with payment failed"

record OrderPlaced(int OrderId);
```

### Event Replay

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();
bus.EnableHistory(maxEvents: 100);

bus.Subscribe<OrderPlaced>((e, _) =>
{
    Console.WriteLine($"Order {e.OrderId}");
    return Task.CompletedTask;
});

await bus.PublishAsync(new OrderPlaced(1));
await bus.PublishAsync(new OrderPlaced(2));
await bus.PublishAsync(new OrderPlaced(3));

// Re-publishes the 2 most recent events (OrderPlaced 2, then 3)
await bus.ReplayLastAsync(2);

record OrderPlaced(int OrderId);
```

### Middleware

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

// Add logging middleware
bus.Use(async (context, next) =>
{
    Console.WriteLine($"Before: {context.EventType.Name}");
    await next();
    Console.WriteLine($"After: {context.EventType.Name}");
});

// Add timing middleware
bus.Use(async (context, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next();
    sw.Stop();
    Console.WriteLine($"Handler took {sw.ElapsedMilliseconds}ms");
});

bus.Subscribe<OrderPlaced>((e, _) =>
{
    Console.WriteLine($"Processing order {e.OrderId}");
    return Task.CompletedTask;
});

await bus.PublishAsync(new OrderPlaced(1));

// Remove every registered middleware when the pipeline should reset
bus.ClearMiddleware();

record OrderPlaced(int OrderId);
```

### One-Time Subscription

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

// Handler is automatically unsubscribed after the first matching event
bus.SubscribeOnce<OrderPlaced>((e, _) =>
{
    Console.WriteLine($"First order placed: {e.OrderId}");
    return Task.CompletedTask;
});

await bus.PublishAsync(new OrderPlaced(1)); // Handled
await bus.PublishAsync(new OrderPlaced(2)); // Ignored — already unsubscribed

record OrderPlaced(int OrderId);
```

### Await Next Event

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

// Start waiting before the event is published
var waitTask = bus.WaitForAsync<OrderPlaced>(
    filter: e => e.Total > 1000);

// Publish events from another part of the application
await bus.PublishAsync(new OrderPlaced(1, 500));   // Skipped by filter
await bus.PublishAsync(new OrderPlaced(2, 2000));  // Completes the wait

var order = await waitTask;
Console.WriteLine($"High-value order: {order.OrderId}");

record OrderPlaced(int OrderId, decimal Total);
```

### Check Subscribers

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

Console.WriteLine(bus.HasSubscribers<OrderPlaced>()); // False
Console.WriteLine(bus.GetSubscriberCount<OrderPlaced>()); // 0

using var sub = bus.Subscribe<OrderPlaced>((_, _) => Task.CompletedTask);
Console.WriteLine(bus.HasSubscribers<OrderPlaced>()); // True
Console.WriteLine(bus.GetSubscriberCount<OrderPlaced>()); // 1

record OrderPlaced(int OrderId);
```

### Bulk Unsubscribe

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

bus.Subscribe<OrderPlaced>((e, _) => { Console.WriteLine(e.OrderId); return Task.CompletedTask; });
bus.Subscribe<OrderPlaced>((e, _) => { Console.WriteLine("Audit: " + e.OrderId); return Task.CompletedTask; });

// Remove all handlers for a specific event type
bus.UnsubscribeAll<OrderPlaced>();

// Or remove all handlers for all event types
bus.UnsubscribeAll();

record OrderPlaced(int OrderId);
```

### Inspect Event History

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();
bus.EnableHistory(100);

await bus.PublishAsync(new OrderPlaced(1));
await bus.PublishAsync(new OrderPlaced(2));
await bus.PublishAsync(new OrderPlaced(3));

// Read recorded events without re-triggering handlers
IReadOnlyList<object> history = bus.GetHistory();
foreach (var evt in history)
{
    Console.WriteLine(((OrderPlaced)evt).OrderId);
}
// Output: 1, 2, 3

// Typed history filtered to a specific event type
IReadOnlyList<OrderPlaced> orders = bus.GetHistory<OrderPlaced>();

record OrderPlaced(int OrderId);
```

### Toggle History

```csharp
using Philiprehberger.EventBus;

var bus = new EventBus();

if (!bus.IsHistoryEnabled)
{
    bus.EnableHistory(100);
}

// ...later, when telemetry is no longer needed...
bus.DisableHistory();
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

// Subscribe an IEventHandler<T> instance directly (no DI required)
var bus = new EventBus();
var handler = new OrderShippedHandler();
using var sub = bus.Subscribe<OrderShipped>(handler);
```

## API

### `IEventBus`

| Method | Description |
|--------|-------------|
| `PublishAsync<T>(@event, ct)` | Publishes an event to all registered handlers for the type |
| `Subscribe<T>(handler, priority, filter)` | Subscribes a handler function; returns `IDisposable` to unsubscribe |
| `Subscribe<T>(IEventHandler<T>, priority, filter)` | Subscribes an `IEventHandler<T>` instance; returns `IDisposable` to unsubscribe |
| `Use(middleware)` | Registers a middleware function that wraps every handler invocation |
| `ClearMiddleware()` | Removes all previously registered middleware functions |
| `EnableHistory(maxEvents)` | Enables circular buffer event history with the specified capacity |
| `DisableHistory()` | Disables history tracking and releases the buffer |
| `IsHistoryEnabled` | `bool` property; `true` when history tracking is currently enabled |
| `ReplayLastAsync(count, ct)` | Re-publishes the N most recent events from the history buffer |
| `ClearHistory()` | Clears all events from the history buffer without disabling tracking |
| `HasSubscribers<T>()` | Returns `true` if any handlers are registered for the event type |
| `GetSubscriberCount<T>()` | Returns the number of handlers registered for the event type |
| `UnsubscribeAll<T>()` | Removes all handlers for a specific event type |
| `UnsubscribeAll()` | Removes all handlers for all event types |
| `GetHistory()` | Returns a read-only snapshot of recorded events in chronological order |
| `GetHistory<T>()` | Returns recorded events filtered to type `T`, oldest first |
| `SubscribeOnce<T>(handler, filter)` | Subscribes a handler that auto-unsubscribes after one invocation |
| `WaitForAsync<T>(filter, ct)` | Returns a `Task<T>` that completes with the next matching event |

### `IEventHandler<T>`

| Method | Description |
|--------|-------------|
| `HandleAsync(@event, ct)` | Handles an event of type `T` asynchronously |

### `EventBusOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ThrowOnHandlerError` | `bool` | `false` | Propagate handler exceptions to the publisher |
| `MaxConcurrency` | `int` | `0` | Max concurrent handler invocations (0 = unlimited); ignored when `SequentialDispatch` is `true` |
| `SequentialDispatch` | `bool` | `false` | Await handlers one at a time in ascending priority order instead of concurrently |
| `OnHandlerError` | `Action<Exception>?` | `null` | Callback invoked when any handler throws an exception |
| `HandlerTimeout` | `TimeSpan?` | `null` | Timeout per handler invocation; throws `TimeoutException` if exceeded |
| `OnDeadLetter` | `Action<object, Exception>?` | `null` | Callback invoked with the failed event and exception when a handler throws and `ThrowOnHandlerError` is `false` |

### `Subscribe<T>` Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `handler` | `Func<T, CancellationToken, Task>` | required | The handler function to invoke |
| `priority` | `int` | `0` | Execution priority; lower values execute first |
| `filter` | `Func<T, bool>?` | `null` | Predicate evaluated before invoking; handler is skipped if it returns `false` |

### `EventContext`

| Property | Type | Description |
|----------|------|-------------|
| `Event` | `object` | The event instance being published |
| `EventType` | `Type` | The CLR type of the event |
| `CancellationToken` | `CancellationToken` | The cancellation token for the current publish operation |
| `Items` | `IDictionary<string, object>` | Dictionary for middleware to pass data along the pipeline |

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
