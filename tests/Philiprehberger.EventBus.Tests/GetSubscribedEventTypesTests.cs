using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class GetSubscribedEventTypesTests
{
    private record TestEvent(string Message);
    private record OtherEvent(int Value);

    [Fact]
    public void GetSubscribedEventTypes_WithNoSubscriptions_ReturnsEmpty()
    {
        var bus = new EventBus();

        Assert.Empty(bus.GetSubscribedEventTypes());
    }

    [Fact]
    public void GetSubscribedEventTypes_ReturnsEachSubscribedTypeOnce()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<OtherEvent>((_, _) => Task.CompletedTask);

        var types = bus.GetSubscribedEventTypes();

        Assert.Equal(2, types.Count);
        Assert.Contains(typeof(TestEvent), types);
        Assert.Contains(typeof(OtherEvent), types);
    }

    [Fact]
    public void GetSubscribedEventTypes_AfterDispose_ExcludesType()
    {
        var bus = new EventBus();
        var sub = bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<OtherEvent>((_, _) => Task.CompletedTask);

        sub.Dispose();

        Assert.Equal(new[] { typeof(OtherEvent) }, bus.GetSubscribedEventTypes());
    }

    [Fact]
    public void GetSubscribedEventTypes_AfterUnsubscribeAll_ReturnsEmpty()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<OtherEvent>((_, _) => Task.CompletedTask);

        bus.UnsubscribeAll();

        Assert.Empty(bus.GetSubscribedEventTypes());
    }
}
