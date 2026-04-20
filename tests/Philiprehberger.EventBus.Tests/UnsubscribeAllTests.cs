using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class UnsubscribeAllTests
{
    private record TestEvent(string Message);
    private record OtherEvent(int Value);

    [Fact]
    public async Task UnsubscribeAllTyped_RemovesHandlersForSpecificType()
    {
        var bus = new EventBus();
        var callCount = 0;
        bus.Subscribe<TestEvent>((_, _) => { callCount++; return Task.CompletedTask; });
        bus.Subscribe<TestEvent>((_, _) => { callCount++; return Task.CompletedTask; });

        bus.UnsubscribeAll<TestEvent>();
        await bus.PublishAsync(new TestEvent("hello"));

        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task UnsubscribeAllTyped_LeavesOtherTypesIntact()
    {
        var bus = new EventBus();
        var testCount = 0;
        var otherCount = 0;
        bus.Subscribe<TestEvent>((_, _) => { testCount++; return Task.CompletedTask; });
        bus.Subscribe<OtherEvent>((_, _) => { otherCount++; return Task.CompletedTask; });

        bus.UnsubscribeAll<TestEvent>();
        await bus.PublishAsync(new TestEvent("hello"));
        await bus.PublishAsync(new OtherEvent(42));

        Assert.Equal(0, testCount);
        Assert.Equal(1, otherCount);
    }

    [Fact]
    public async Task UnsubscribeAll_RemovesAllHandlersForAllTypes()
    {
        var bus = new EventBus();
        var testCount = 0;
        var otherCount = 0;
        bus.Subscribe<TestEvent>((_, _) => { testCount++; return Task.CompletedTask; });
        bus.Subscribe<OtherEvent>((_, _) => { otherCount++; return Task.CompletedTask; });

        bus.UnsubscribeAll();
        await bus.PublishAsync(new TestEvent("hello"));
        await bus.PublishAsync(new OtherEvent(42));

        Assert.Equal(0, testCount);
        Assert.Equal(0, otherCount);
    }

    [Fact]
    public void UnsubscribeAllTyped_WhenNoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();

        bus.UnsubscribeAll<TestEvent>();
    }
}
