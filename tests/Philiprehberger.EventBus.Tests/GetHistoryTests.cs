using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class GetHistoryTests
{
    private record TestEvent(string Message);

    [Fact]
    public void GetHistory_WhenHistoryNotEnabled_ThrowsInvalidOperationException()
    {
        var bus = new EventBus();

        Assert.Throws<InvalidOperationException>(() => bus.GetHistory());
    }

    [Fact]
    public void GetHistory_WithNoEvents_ReturnsEmptyList()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        var history = bus.GetHistory();

        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistory_ReturnsEventsInChronologicalOrder()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        await bus.PublishAsync(new TestEvent("first"));
        await bus.PublishAsync(new TestEvent("second"));
        await bus.PublishAsync(new TestEvent("third"));

        var history = bus.GetHistory();

        Assert.Equal(3, history.Count);
        Assert.Equal("first", ((TestEvent)history[0]).Message);
        Assert.Equal("second", ((TestEvent)history[1]).Message);
        Assert.Equal("third", ((TestEvent)history[2]).Message);
    }

    [Fact]
    public async Task GetHistory_RespectsBufferCapacity()
    {
        var bus = new EventBus();
        bus.EnableHistory(2);

        await bus.PublishAsync(new TestEvent("first"));
        await bus.PublishAsync(new TestEvent("second"));
        await bus.PublishAsync(new TestEvent("third"));

        var history = bus.GetHistory();

        Assert.Equal(2, history.Count);
        Assert.Equal("second", ((TestEvent)history[0]).Message);
        Assert.Equal("third", ((TestEvent)history[1]).Message);
    }
}
