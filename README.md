# Philiprehberger.EventBus

[![CI](https://github.com/philiprehberger/dotnet-event-bus/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-event-bus/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.EventBus.svg)](https://www.nuget.org/packages/Philiprehberger.EventBus)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-event-bus)](LICENSE)
[![Sponsor](https://img.shields.io/badge/sponsor-GitHub%20Sponsors-ec6cb9)](https://github.com/sponsors/philiprehberger)

In-process publish/subscribe event bus with async handlers, scoped subscriptions, and Microsoft DI integration.

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

### DI Registration

```csharp
using Philiprehberger.EventBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventBus(options => new EventBusOptions(
    ThrowOnHandlerError: true,
    MaxConcurrency: 4
));

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
| `Subscribe<T>(handler)` | Subscribes a handler function; returns `IDisposable` to unsubscribe |

### `IEventHandler<T>`

| Method | Description |
|--------|-------------|
| `HandleAsync(@event, ct)` | Handles an event of type `T` asynchronously |

### `EventBusOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ThrowOnHandlerError` | `bool` | `false` | Propagate handler exceptions to the publisher |
| `MaxConcurrency` | `int` | `0` | Max concurrent handler invocations (0 = unlimited) |

### `ServiceCollectionExtensions`

| Method | Description |
|--------|-------------|
| `AddEventBus(configure?)` | Registers `IEventBus` as singleton and scans for `IEventHandler<T>` implementations |

## Development

```bash
dotnet build src/Philiprehberger.EventBus.csproj --configuration Release
```

## License

[MIT](LICENSE)
