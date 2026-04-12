using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class HasSubscribersTests
{
    private record TestEvent(string Message);
    private record OtherEvent(int Value);

    [Fact]
    public void HasSubscribers_WithNoSubscriptions_ReturnsFalse()
    {
        var bus = new EventBus();

        Assert.False(bus.HasSubscribers<TestEvent>());
    }

    [Fact]
    public void HasSubscribers_WithSubscription_ReturnsTrue()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        Assert.True(bus.HasSubscribers<TestEvent>());
    }

    [Fact]
    public void HasSubscribers_AfterDispose_ReturnsFalse()
    {
        var bus = new EventBus();
        var sub = bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        sub.Dispose();

        Assert.False(bus.HasSubscribers<TestEvent>());
    }

    [Fact]
    public void HasSubscribers_DifferentType_ReturnsFalse()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        Assert.False(bus.HasSubscribers<OtherEvent>());
    }
}
