using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class EventReplayTests
{
    private record TestEvent(string Message);
    private record OtherEvent(int Value);

    [Fact]
    public async Task ReplayLastAsync_ReplaysEventsInOrder()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        var received = new List<string>();
        bus.Subscribe<TestEvent>((e, _) => { received.Add(e.Message); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("a"));
        await bus.PublishAsync(new TestEvent("b"));
        await bus.PublishAsync(new TestEvent("c"));

        received.Clear();
        await bus.ReplayLastAsync(2);

        Assert.Equal(new[] { "b", "c" }, received);
    }

    [Fact]
    public async Task ReplayLastAsync_WithCountExceedingHistory_ReplaysAll()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        var received = new List<string>();
        bus.Subscribe<TestEvent>((e, _) => { received.Add(e.Message); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("a"));
        await bus.PublishAsync(new TestEvent("b"));

        received.Clear();
        await bus.ReplayLastAsync(100);

        Assert.Equal(new[] { "a", "b" }, received);
    }

    [Fact]
    public async Task ReplayLastAsync_WithCircularBufferOverflow_KeepsOnlyLatest()
    {
        var bus = new EventBus();
        bus.EnableHistory(3);

        var received = new List<string>();
        bus.Subscribe<TestEvent>((e, _) => { received.Add(e.Message); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("a"));
        await bus.PublishAsync(new TestEvent("b"));
        await bus.PublishAsync(new TestEvent("c"));
        await bus.PublishAsync(new TestEvent("d"));
        await bus.PublishAsync(new TestEvent("e"));

        received.Clear();
        await bus.ReplayLastAsync(10);

        Assert.Equal(new[] { "c", "d", "e" }, received);
    }

    [Fact]
    public async Task ReplayLastAsync_WithZeroCount_ReplaysNothing()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        var received = new List<string>();
        bus.Subscribe<TestEvent>((e, _) => { received.Add(e.Message); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("a"));

        received.Clear();
        await bus.ReplayLastAsync(0);

        Assert.Empty(received);
    }

    [Fact]
    public async Task ReplayLastAsync_WithoutEnableHistory_ThrowsInvalidOperationException()
    {
        var bus = new EventBus();

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.ReplayLastAsync(1));
    }

    [Fact]
    public void EnableHistory_WithZeroOrNegative_ThrowsArgumentOutOfRangeException()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentOutOfRangeException>(() => bus.EnableHistory(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => bus.EnableHistory(-1));
    }

    [Fact]
    public async Task ReplayLastAsync_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => bus.ReplayLastAsync(-1));
    }

    [Fact]
    public async Task ReplayLastAsync_WithMixedEventTypes_ReplaysCorrectTypes()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        var testReceived = new List<string>();
        var otherReceived = new List<int>();

        bus.Subscribe<TestEvent>((e, _) => { testReceived.Add(e.Message); return Task.CompletedTask; });
        bus.Subscribe<OtherEvent>((e, _) => { otherReceived.Add(e.Value); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("hello"));
        await bus.PublishAsync(new OtherEvent(42));
        await bus.PublishAsync(new TestEvent("world"));

        testReceived.Clear();
        otherReceived.Clear();
        await bus.ReplayLastAsync(3);

        Assert.Equal(new[] { "hello", "world" }, testReceived);
        Assert.Equal(new[] { 42 }, otherReceived);
    }

    [Fact]
    public async Task ReplayLastAsync_EmptyHistory_ReplaysNothing()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        var received = new List<string>();
        bus.Subscribe<TestEvent>((e, _) => { received.Add(e.Message); return Task.CompletedTask; });

        await bus.ReplayLastAsync(5);

        Assert.Empty(received);
    }
}
