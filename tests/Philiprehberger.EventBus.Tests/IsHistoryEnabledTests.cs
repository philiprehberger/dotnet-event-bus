using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class IsHistoryEnabledTests
{
    private record TestEvent(int Id);

    [Fact]
    public void IsHistoryEnabled_Default_ReturnsFalse()
    {
        var bus = new EventBus();
        Assert.False(bus.IsHistoryEnabled);
    }

    [Fact]
    public void IsHistoryEnabled_AfterEnable_ReturnsTrue()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);
        Assert.True(bus.IsHistoryEnabled);
    }

    [Fact]
    public void IsHistoryEnabled_AfterDisable_ReturnsFalse()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);
        bus.DisableHistory();
        Assert.False(bus.IsHistoryEnabled);
    }

    [Fact]
    public async Task IsHistoryEnabled_AfterPublish_StillTrue()
    {
        var bus = new EventBus();
        bus.EnableHistory(5);
        await bus.PublishAsync(new TestEvent(1));
        Assert.True(bus.IsHistoryEnabled);
    }
}
