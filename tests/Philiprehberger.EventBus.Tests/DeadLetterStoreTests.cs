using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class DeadLetterStoreTests
{
    private record TestEvent(string Message);

    [Fact]
    public async Task GetDeadLetters_WhenStoreDisabled_ReturnsEmpty()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Empty(bus.GetDeadLetters());
    }

    [Fact]
    public async Task GetDeadLetters_WhenHandlerThrows_RecordsEventExceptionAndTimestamp()
    {
        var bus = new EventBus(new EventBusOptions { MaxDeadLetters = 10 });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("oops"));

        var before = DateTimeOffset.UtcNow;
        await bus.PublishAsync(new TestEvent("fail"));
        var after = DateTimeOffset.UtcNow;

        var deadLetter = Assert.Single(bus.GetDeadLetters());
        Assert.Equal(new TestEvent("fail"), deadLetter.Event);
        Assert.IsType<InvalidOperationException>(deadLetter.Exception);
        Assert.Equal("oops", deadLetter.Exception.Message);
        Assert.InRange(deadLetter.Timestamp, before, after);
    }

    [Fact]
    public async Task GetDeadLetters_WhenCapacityExceeded_KeepsMostRecentInOrder()
    {
        var bus = new EventBus(new EventBusOptions { MaxDeadLetters = 2 });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("1"));
        await bus.PublishAsync(new TestEvent("2"));
        await bus.PublishAsync(new TestEvent("3"));

        var deadLetters = bus.GetDeadLetters();
        Assert.Equal(2, deadLetters.Count);
        Assert.Equal("2", ((TestEvent)deadLetters[0].Event).Message);
        Assert.Equal("3", ((TestEvent)deadLetters[1].Event).Message);
    }

    [Fact]
    public async Task GetDeadLetters_WorksAlongsideOnDeadLetter()
    {
        var callbackCount = 0;
        var bus = new EventBus(new EventBusOptions
        {
            MaxDeadLetters = 5,
            OnDeadLetter = (_, _) => callbackCount++
        });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(1, callbackCount);
        Assert.Single(bus.GetDeadLetters());
    }

    [Fact]
    public async Task GetDeadLetters_WhenThrowOnHandlerErrorTrue_DoesNotRecord()
    {
        var bus = new EventBus(new EventBusOptions
        {
            MaxDeadLetters = 5,
            ThrowOnHandlerError = true
        });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent("test")));

        Assert.Empty(bus.GetDeadLetters());
    }

    [Fact]
    public async Task GetDeadLetters_WhenNoError_ReturnsEmpty()
    {
        var bus = new EventBus(new EventBusOptions { MaxDeadLetters = 5 });
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("ok"));

        Assert.Empty(bus.GetDeadLetters());
    }

    [Fact]
    public async Task GetDeadLetters_ReturnsSnapshot()
    {
        var bus = new EventBus(new EventBusOptions { MaxDeadLetters = 5 });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("1"));
        var snapshot = bus.GetDeadLetters();
        await bus.PublishAsync(new TestEvent("2"));

        Assert.Single(snapshot);
        Assert.Equal(2, bus.GetDeadLetters().Count);
    }

    [Fact]
    public async Task GetDeadLetters_UnderConcurrentFailures_RecordsUpToCapacity()
    {
        var bus = new EventBus(new EventBusOptions { MaxDeadLetters = 50 });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await Task.WhenAll(Enumerable.Range(0, 200).Select(i => bus.PublishAsync(new TestEvent(i.ToString()))));

        Assert.Equal(50, bus.GetDeadLetters().Count);
    }

    [Fact]
    public async Task ClearDeadLetters_RemovesAllEntries()
    {
        var bus = new EventBus(new EventBusOptions { MaxDeadLetters = 5 });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("1"));
        await bus.PublishAsync(new TestEvent("2"));
        bus.ClearDeadLetters();

        Assert.Empty(bus.GetDeadLetters());

        await bus.PublishAsync(new TestEvent("3"));
        var deadLetter = Assert.Single(bus.GetDeadLetters());
        Assert.Equal("3", ((TestEvent)deadLetter.Event).Message);
    }

    [Fact]
    public void ClearDeadLetters_WhenStoreDisabled_DoesNotThrow()
    {
        var bus = new EventBus();

        bus.ClearDeadLetters();

        Assert.Empty(bus.GetDeadLetters());
    }

    [Fact]
    public void Constructor_WithNegativeMaxDeadLetters_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventBus(new EventBusOptions { MaxDeadLetters = -1 }));
    }
}
