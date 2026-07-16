using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class SequentialDispatchTests
{
    private record TestEvent(int Value);

    [Fact]
    public async Task SequentialDispatch_HandlersRunOneAtATimeInPriorityOrder()
    {
        var bus = new EventBus(new EventBusOptions { SequentialDispatch = true });
        var log = new List<string>();
        var active = 0;
        var maxConcurrent = 0;

        async Task Handler(string name, int delayMs)
        {
            var current = Interlocked.Increment(ref active);
            maxConcurrent = Math.Max(maxConcurrent, current);
            await Task.Delay(delayMs);
            log.Add(name);
            Interlocked.Decrement(ref active);
        }

        // Registered out of priority order to prove ordering is by priority, not registration.
        bus.Subscribe<TestEvent>(async (_, _) => await Handler("third", 5), priority: 30);
        bus.Subscribe<TestEvent>(async (_, _) => await Handler("first", 30), priority: 10);
        bus.Subscribe<TestEvent>(async (_, _) => await Handler("second", 5), priority: 20);

        await bus.PublishAsync(new TestEvent(1));

        Assert.Equal(new[] { "first", "second", "third" }, log);
        Assert.Equal(1, maxConcurrent);
    }

    [Fact]
    public async Task SequentialDispatch_False_DispatchesConcurrently()
    {
        var bus = new EventBus(new EventBusOptions { SequentialDispatch = false });
        var active = 0;
        var maxConcurrent = 0;

        Task Handler()
        {
            return Task.Run(async () =>
            {
                var current = Interlocked.Increment(ref active);
                maxConcurrent = Math.Max(maxConcurrent, current);
                await Task.Delay(30);
                Interlocked.Decrement(ref active);
            });
        }

        bus.Subscribe<TestEvent>((_, _) => Handler());
        bus.Subscribe<TestEvent>((_, _) => Handler());

        await bus.PublishAsync(new TestEvent(1));

        Assert.Equal(2, maxConcurrent);
    }

    [Fact]
    public async Task SequentialDispatch_StopsRemainingHandlersWhenOneThrowsAndThrowOnHandlerError()
    {
        var bus = new EventBus(new EventBusOptions
        {
            SequentialDispatch = true,
            ThrowOnHandlerError = true
        });
        var secondRan = false;

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("boom"), priority: 10);
        bus.Subscribe<TestEvent>((_, _) => { secondRan = true; return Task.CompletedTask; }, priority: 20);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent(1)));
        Assert.False(secondRan);
    }

    [Fact]
    public async Task SequentialDispatch_ContinuesAfterSwallowedError()
    {
        var bus = new EventBus(new EventBusOptions
        {
            SequentialDispatch = true,
            ThrowOnHandlerError = false
        });
        var secondRan = false;

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("boom"), priority: 10);
        bus.Subscribe<TestEvent>((_, _) => { secondRan = true; return Task.CompletedTask; }, priority: 20);

        await bus.PublishAsync(new TestEvent(1));

        Assert.True(secondRan);
    }

    [Fact]
    public async Task SequentialDispatch_RespectsFilters()
    {
        var bus = new EventBus(new EventBusOptions { SequentialDispatch = true });
        var log = new List<string>();

        bus.Subscribe<TestEvent>((_, _) => { log.Add("a"); return Task.CompletedTask; }, filter: e => e.Value > 0);
        bus.Subscribe<TestEvent>((_, _) => { log.Add("b"); return Task.CompletedTask; }, filter: e => e.Value < 0);

        await bus.PublishAsync(new TestEvent(1));

        Assert.Equal(new[] { "a" }, log);
    }
}
