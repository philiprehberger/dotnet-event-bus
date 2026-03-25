using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class EventBusTests
{
    private record TestEvent(string Message);

    [Fact]
    public async Task PublishAsync_WithSubscriber_InvokesHandler()
    {
        var bus = new EventBus();
        string? received = null;
        bus.Subscribe<TestEvent>((e, _) =>
        {
            received = e.Message;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("hello"));

        Assert.Equal("hello", received);
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();

        await bus.PublishAsync(new TestEvent("hello"));
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        var bus = new EventBus();

        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync<TestEvent>(null!));
    }

    [Fact]
    public async Task Subscribe_Dispose_UnsubscribesHandler()
    {
        var bus = new EventBus();
        var callCount = 0;
        var sub = bus.Subscribe<TestEvent>((_, _) =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("first"));
        sub.Dispose();
        await bus.PublishAsync(new TestEvent("second"));

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task PublishAsync_WithThrowOnHandlerError_PropagatesException()
    {
        var bus = new EventBus(new EventBusOptions(ThrowOnHandlerError: true));
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent("test")));
    }

    [Fact]
    public async Task PublishAsync_WithoutThrowOnHandlerError_SwallowsException()
    {
        var bus = new EventBus(new EventBusOptions(ThrowOnHandlerError: false));
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("test"));
    }
}
