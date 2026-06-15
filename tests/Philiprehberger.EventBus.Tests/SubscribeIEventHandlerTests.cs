using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class SubscribeIEventHandlerTests
{
    private record OrderPlaced(int OrderId);

    private sealed class CapturingHandler : IEventHandler<OrderPlaced>
    {
        public List<OrderPlaced> Received { get; } = new();

        public Task HandleAsync(OrderPlaced @event, CancellationToken ct)
        {
            Received.Add(@event);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Subscribe_WithIEventHandler_ReceivesEvents()
    {
        var bus = new EventBus();
        var handler = new CapturingHandler();
        using var sub = bus.Subscribe<OrderPlaced>(handler);

        await bus.PublishAsync(new OrderPlaced(1));
        await bus.PublishAsync(new OrderPlaced(2));

        Assert.Equal(2, handler.Received.Count);
        Assert.Equal(1, handler.Received[0].OrderId);
        Assert.Equal(2, handler.Received[1].OrderId);
    }

    [Fact]
    public async Task Subscribe_WithIEventHandler_DisposeUnsubscribes()
    {
        var bus = new EventBus();
        var handler = new CapturingHandler();
        var sub = bus.Subscribe<OrderPlaced>(handler);

        await bus.PublishAsync(new OrderPlaced(1));
        sub.Dispose();
        await bus.PublishAsync(new OrderPlaced(2));

        Assert.Single(handler.Received);
        Assert.Equal(1, handler.Received[0].OrderId);
    }

    [Fact]
    public async Task Subscribe_WithIEventHandler_RespectsFilter()
    {
        var bus = new EventBus();
        var handler = new CapturingHandler();
        using var sub = bus.Subscribe<OrderPlaced>(handler, filter: e => e.OrderId > 1);

        await bus.PublishAsync(new OrderPlaced(1));
        await bus.PublishAsync(new OrderPlaced(2));

        Assert.Single(handler.Received);
        Assert.Equal(2, handler.Received[0].OrderId);
    }

    [Fact]
    public void Subscribe_WithNullHandler_Throws()
    {
        var bus = new EventBus();
        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<OrderPlaced>((IEventHandler<OrderPlaced>)null!));
    }
}
