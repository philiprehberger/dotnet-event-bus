using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class GetSubscriberCountTests
{
    private record TestEvent(string Message);
    private record OtherEvent(int Value);

    [Fact]
    public void GetSubscriberCount_WithNoSubscribers_ReturnsZero()
    {
        var bus = new EventBus();

        Assert.Equal(0, bus.GetSubscriberCount<TestEvent>());
    }

    [Fact]
    public void GetSubscriberCount_WithSubscribers_ReturnsCorrectCount()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        Assert.Equal(3, bus.GetSubscriberCount<TestEvent>());
    }

    [Fact]
    public void GetSubscriberCount_AfterDispose_Decrements()
    {
        var bus = new EventBus();
        var sub1 = bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        Assert.Equal(2, bus.GetSubscriberCount<TestEvent>());

        sub1.Dispose();

        Assert.Equal(1, bus.GetSubscriberCount<TestEvent>());
    }

    [Fact]
    public void GetSubscriberCount_ForUnregisteredType_ReturnsZero()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        Assert.Equal(0, bus.GetSubscriberCount<OtherEvent>());
    }
}
