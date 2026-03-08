using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class SubscribeOnceTests
{
    private record TestEvent(string Message);

    [Fact]
    public async Task SubscribeOnce_InvokesHandlerOnce()
    {
        var bus = new EventBus();
        var count = 0;
        bus.SubscribeOnce<TestEvent>((_, _) =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("first"));
        await bus.PublishAsync(new TestEvent("second"));
        await bus.PublishAsync(new TestEvent("third"));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SubscribeOnce_ReceivesCorrectEvent()
    {
        var bus = new EventBus();
        string? received = null;
        bus.SubscribeOnce<TestEvent>((e, _) =>
        {
            received = e.Message;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("hello"));

        Assert.Equal("hello", received);
    }

    [Fact]
    public async Task SubscribeOnce_WithFilter_SkipsNonMatching()
    {
        var bus = new EventBus();
        string? received = null;
        bus.SubscribeOnce<TestEvent>((e, _) =>
        {
            received = e.Message;
            return Task.CompletedTask;
        }, filter: e => e.Message.StartsWith("target"));

        await bus.PublishAsync(new TestEvent("ignore"));
        await bus.PublishAsync(new TestEvent("target-hit"));
        await bus.PublishAsync(new TestEvent("target-miss"));

        Assert.Equal("target-hit", received);
    }

    [Fact]
    public async Task SubscribeOnce_DisposeBeforeEvent_PreventsInvocation()
    {
        var bus = new EventBus();
        var invoked = false;
        var sub = bus.SubscribeOnce<TestEvent>((_, _) =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        sub.Dispose();
        await bus.PublishAsync(new TestEvent("hello"));

        Assert.False(invoked);
    }

    [Fact]
    public void SubscribeOnce_NullHandler_ThrowsArgumentNullException()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() =>
            bus.SubscribeOnce<TestEvent>(null!));
    }

    [Fact]
    public async Task SubscribeOnce_UnsubscribesFromBus()
    {
        var bus = new EventBus();
        bus.SubscribeOnce<TestEvent>((_, _) => Task.CompletedTask);

        Assert.True(bus.HasSubscribers<TestEvent>());

        await bus.PublishAsync(new TestEvent("trigger"));

        Assert.False(bus.HasSubscribers<TestEvent>());
    }
}
