using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class ClearHistoryTests
{
    private record TestEvent(int Value);

    [Fact]
    public void ClearHistory_WithoutEnableHistory_ThrowsInvalidOperationException()
    {
        var bus = new EventBus();

        Assert.Throws<InvalidOperationException>(() => bus.ClearHistory());
    }

    [Fact]
    public async Task ClearHistory_RemovesAllEvents()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        await bus.PublishAsync(new TestEvent(1));
        await bus.PublishAsync(new TestEvent(2));

        bus.ClearHistory();

        var replayed = new List<int>();
        bus.Subscribe<TestEvent>((e, _) =>
        {
            replayed.Add(e.Value);
            return Task.CompletedTask;
        });

        await bus.ReplayLastAsync(10);

        Assert.Empty(replayed);
    }

    [Fact]
    public async Task ClearHistory_StillRecordsNewEvents()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        await bus.PublishAsync(new TestEvent(1));
        bus.ClearHistory();
        await bus.PublishAsync(new TestEvent(2));

        var replayed = new List<int>();
        bus.Subscribe<TestEvent>((e, _) =>
        {
            replayed.Add(e.Value);
            return Task.CompletedTask;
        });

        await bus.ReplayLastAsync(10);

        Assert.Single(replayed);
        Assert.Equal(2, replayed[0]);
    }
}
