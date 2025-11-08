using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class DeadLetterTests
{
    private record TestEvent(string Message);

    [Fact]
    public async Task OnDeadLetter_WhenHandlerThrowsAndErrorsSwallowed_InvokesCallback()
    {
        var deadLetters = new List<(object Event, Exception Exception)>();
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = false,
            OnDeadLetter = (evt, ex) => deadLetters.Add((evt, ex))
        });

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("oops"));

        await bus.PublishAsync(new TestEvent("fail"));

        Assert.Single(deadLetters);
        var (evt, ex) = deadLetters[0];
        Assert.IsType<TestEvent>(evt);
        Assert.Equal("fail", ((TestEvent)evt).Message);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal("oops", ex.Message);
    }

    [Fact]
    public async Task OnDeadLetter_WhenThrowOnHandlerErrorTrue_DoesNotInvokeCallback()
    {
        var deadLetterInvoked = false;
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = true,
            OnDeadLetter = (_, _) => deadLetterInvoked = true
        });

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent("test")));

        Assert.False(deadLetterInvoked);
    }

    [Fact]
    public async Task OnDeadLetter_WhenNoError_DoesNotInvokeCallback()
    {
        var deadLetterInvoked = false;
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = false,
            OnDeadLetter = (_, _) => deadLetterInvoked = true
        });

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("ok"));

        Assert.False(deadLetterInvoked);
    }

    [Fact]
    public async Task OnDeadLetter_WhenNotConfigured_DoesNotThrow()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = false,
            OnDeadLetter = null
        });

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("fail"));

        await bus.PublishAsync(new TestEvent("test"));
    }

    [Fact]
    public async Task OnDeadLetter_WithMultipleFailingHandlers_InvokesForEach()
    {
        var deadLetters = new List<(object Event, Exception Exception)>();
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnHandlerError = false,
            OnDeadLetter = (evt, ex) => deadLetters.Add((evt, ex))
        });

        bus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("first"));
        bus.Subscribe<TestEvent>((_, _) => throw new ArgumentException("second"));

        await bus.PublishAsync(new TestEvent("fail"));

        Assert.Equal(2, deadLetters.Count);
    }
}
