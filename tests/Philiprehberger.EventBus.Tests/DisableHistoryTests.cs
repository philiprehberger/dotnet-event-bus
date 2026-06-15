using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class DisableHistoryTests
{
    private record TestEvent(int Id);

    [Fact]
    public async Task DisableHistory_AfterEnable_GetHistoryThrows()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);
        await bus.PublishAsync(new TestEvent(1));

        bus.DisableHistory();

        Assert.Throws<InvalidOperationException>(() => bus.GetHistory());
    }

    [Fact]
    public void DisableHistory_WhenNotEnabled_DoesNotThrow()
    {
        var bus = new EventBus();
        bus.DisableHistory();
        Assert.False(bus.IsHistoryEnabled);
    }

    [Fact]
    public async Task DisableHistory_PreventsFurtherTracking()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        await bus.PublishAsync(new TestEvent(1));
        bus.DisableHistory();
        bus.EnableHistory(10);
        await bus.PublishAsync(new TestEvent(2));

        var history = bus.GetHistory();
        Assert.Single(history);
        Assert.Equal(new TestEvent(2), history[0]);
    }

    [Fact]
    public async Task DisableHistory_ReplayLastAsyncThrows()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);
        await bus.PublishAsync(new TestEvent(1));
        bus.DisableHistory();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await bus.ReplayLastAsync(1));
    }
}
