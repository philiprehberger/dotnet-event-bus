using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class EventBusTests
{
    private record TestEvent(string Message);
    private record PriorityEvent(int Value);

    [Fact]
    public async Task PublishAsync_WithSubscriber_InvokesHandler()
    {
        var bus = new EventBus();
        string? received = null;
        bus.Subscribe<TestEvent>((e, _) =>
        {
            received = e.Message;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("hello"));

        Assert.Equal("hello", received);
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();

        await bus.PublishAsync(new TestEvent("hello"));
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        var bus = new EventBus();

        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync<TestEvent>(null!));
    }

    [Fact]
    public async Task Subscribe_Dispose_UnsubscribesHandler()
    {
        var bus = new EventBus();
        var callCount = 0;
        var sub = bus.Subscribe<TestEvent>((_, _) =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("first"));
        sub.Dispose();
        await bus.PublishAsync(new TestEvent("second"));

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task PublishAsync_WithThrowOnHandlerError_PropagatesException()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnHandlerError = true });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent("test")));
    }

    [Fact]
    public async Task PublishAsync_WithoutThrowOnHandlerError_SwallowsException()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnHandlerError = false });
        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("test"));
    }

    // --- Priority/Ordering Tests ---

    [Fact]
    public async Task Subscribe_WithPriority_HandlersExecuteInPriorityOrder()
    {
        var bus = new EventBus();
        var order = new List<int>();

        bus.Subscribe<PriorityEvent>((_, _) => { order.Add(2); return Task.CompletedTask; }, priority: 20);
        bus.Subscribe<PriorityEvent>((_, _) => { order.Add(1); return Task.CompletedTask; }, priority: 10);
        bus.Subscribe<PriorityEvent>((_, _) => { order.Add(3); return Task.CompletedTask; }, priority: 30);

        await bus.PublishAsync(new PriorityEvent(0));

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public async Task Subscribe_WithSamePriority_MaintainsRegistrationOrder()
    {
        var bus = new EventBus();
        var order = new List<string>();

        bus.Subscribe<TestEvent>((_, _) => { order.Add("first"); return Task.CompletedTask; }, priority: 0);
        bus.Subscribe<TestEvent>((_, _) => { order.Add("second"); return Task.CompletedTask; }, priority: 0);
        bus.Subscribe<TestEvent>((_, _) => { order.Add("third"); return Task.CompletedTask; }, priority: 0);

        await bus.PublishAsync(new TestEvent("go"));

        Assert.Equal(new[] { "first", "second", "third" }, order);
    }

    [Fact]
    public async Task Subscribe_WithNegativePriority_ExecutesBeforeDefault()
    {
        var bus = new EventBus();
        var order = new List<string>();

        bus.Subscribe<TestEvent>((_, _) => { order.Add("default"); return Task.CompletedTask; }, priority: 0);
        bus.Subscribe<TestEvent>((_, _) => { order.Add("early"); return Task.CompletedTask; }, priority: -10);

        await bus.PublishAsync(new TestEvent("go"));

        Assert.Equal(new[] { "early", "default" }, order);
    }

    // --- Filter Tests ---

    [Fact]
    public async Task Subscribe_WithFilter_SkipsHandlerWhenFilterReturnsFalse()
    {
        var bus = new EventBus();
        var received = new List<string>();

        bus.Subscribe<TestEvent>(
            (e, _) => { received.Add(e.Message); return Task.CompletedTask; },
            filter: e => e.Message.StartsWith("keep"));

        await bus.PublishAsync(new TestEvent("keep-this"));
        await bus.PublishAsync(new TestEvent("skip-this"));

        Assert.Single(received);
        Assert.Equal("keep-this", received[0]);
    }

    [Fact]
    public async Task Subscribe_WithFilter_InvokesHandlerWhenFilterReturnsTrue()
    {
        var bus = new EventBus();
        var callCount = 0;

        bus.Subscribe<PriorityEvent>(
            (_, _) => { callCount++; return Task.CompletedTask; },
            filter: e => e.Value > 5);

        await bus.PublishAsync(new PriorityEvent(10));
        await bus.PublishAsync(new PriorityEvent(20));

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Subscribe_WithoutFilter_InvokesHandlerForAllEvents()
    {
        var bus = new EventBus();
        var callCount = 0;

        bus.Subscribe<TestEvent>((_, _) => { callCount++; return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("a"));
        await bus.PublishAsync(new TestEvent("b"));
        await bus.PublishAsync(new TestEvent("c"));

        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task Subscribe_WithFilterAndPriority_AppliesBoth()
    {
        var bus = new EventBus();
        var order = new List<int>();

        bus.Subscribe<PriorityEvent>(
            (e, _) => { order.Add(e.Value); return Task.CompletedTask; },
            priority: 10,
            filter: e => e.Value % 2 == 0);

        bus.Subscribe<PriorityEvent>(
            (e, _) => { order.Add(e.Value * 100); return Task.CompletedTask; },
            priority: 5,
            filter: e => e.Value % 2 == 0);

        await bus.PublishAsync(new PriorityEvent(2));
        await bus.PublishAsync(new PriorityEvent(3));

        Assert.Equal(new[] { 200, 2 }, order);
    }

    // --- OnHandlerError Callback Tests ---

    [Fact]
    public async Task OnHandlerError_WhenHandlerThrows_InvokesCallback()
    {
        var capturedErrors = new List<Exception>();
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = false,
            OnHandlerError = ex => capturedErrors.Add(ex)
        });

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("oops"));

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Single(capturedErrors);
        Assert.IsType<InvalidOperationException>(capturedErrors[0]);
        Assert.Equal("oops", capturedErrors[0].Message);
    }

    [Fact]
    public async Task OnHandlerError_WithThrowOnHandlerError_CallbackStillInvoked()
    {
        var capturedErrors = new List<Exception>();
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = true,
            OnHandlerError = ex => capturedErrors.Add(ex)
        });

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent("test")));

        Assert.Single(capturedErrors);
    }

    [Fact]
    public async Task OnHandlerError_WhenNoError_CallbackNotInvoked()
    {
        var callbackInvoked = false;
        var bus = new EventBus(new EventBusOptions
        {
            OnHandlerError = _ => callbackInvoked = true
        });

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("test"));

        Assert.False(callbackInvoked);
    }

    // --- Handler Timeout Tests ---

    [Fact]
    public async Task HandlerTimeout_WhenHandlerExceedsTimeout_ThrowsTimeoutException()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = true,
            HandlerTimeout = TimeSpan.FromMilliseconds(50)
        });

        bus.Subscribe<TestEvent>(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        });

        await Assert.ThrowsAsync<TimeoutException>(() => bus.PublishAsync(new TestEvent("slow")));
    }

    [Fact]
    public async Task HandlerTimeout_WhenHandlerCompletesInTime_Succeeds()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = true,
            HandlerTimeout = TimeSpan.FromSeconds(5)
        });

        string? received = null;
        bus.Subscribe<TestEvent>((e, _) =>
        {
            received = e.Message;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("fast"));

        Assert.Equal("fast", received);
    }

    [Fact]
    public async Task HandlerTimeout_WhenNotConfigured_NoTimeoutEnforced()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = true,
            HandlerTimeout = null
        });

        var completed = false;
        bus.Subscribe<TestEvent>((_, _) =>
        {
            completed = true;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.True(completed);
    }

    [Fact]
    public async Task HandlerTimeout_TimeoutError_InvokesOnHandlerErrorCallback()
    {
        var capturedErrors = new List<Exception>();
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = false,
            HandlerTimeout = TimeSpan.FromMilliseconds(50),
            OnHandlerError = ex => capturedErrors.Add(ex)
        });

        bus.Subscribe<TestEvent>(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        });

        await bus.PublishAsync(new TestEvent("slow"));

        Assert.Single(capturedErrors);
        Assert.IsType<TimeoutException>(capturedErrors[0]);
    }
}
