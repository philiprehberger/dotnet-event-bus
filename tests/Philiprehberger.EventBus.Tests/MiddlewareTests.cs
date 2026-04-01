using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class MiddlewareTests
{
    private record TestEvent(string Message);

    [Fact]
    public async Task Use_MiddlewareWrapsHandlerInvocation()
    {
        var bus = new EventBus();
        var log = new List<string>();

        bus.Use(async (context, next) =>
        {
            log.Add("before");
            await next();
            log.Add("after");
        });

        bus.Subscribe<TestEvent>((_, _) => { log.Add("handler"); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(new[] { "before", "handler", "after" }, log);
    }

    [Fact]
    public async Task Use_MultipleMiddleware_ExecutesInRegistrationOrder()
    {
        var bus = new EventBus();
        var log = new List<string>();

        bus.Use(async (_, next) =>
        {
            log.Add("m1-before");
            await next();
            log.Add("m1-after");
        });

        bus.Use(async (_, next) =>
        {
            log.Add("m2-before");
            await next();
            log.Add("m2-after");
        });

        bus.Subscribe<TestEvent>((_, _) => { log.Add("handler"); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(new[] { "m1-before", "m2-before", "handler", "m2-after", "m1-after" }, log);
    }

    [Fact]
    public async Task Use_MiddlewareReceivesCorrectEventContext()
    {
        var bus = new EventBus();
        EventContext? capturedContext = null;

        bus.Use(async (context, next) =>
        {
            capturedContext = context;
            await next();
        });

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("hello"));

        Assert.NotNull(capturedContext);
        Assert.IsType<TestEvent>(capturedContext!.Event);
        Assert.Equal("hello", ((TestEvent)capturedContext.Event).Message);
        Assert.Equal(typeof(TestEvent), capturedContext.EventType);
    }

    [Fact]
    public async Task Use_MiddlewareCanShortCircuit()
    {
        var bus = new EventBus();
        var handlerCalled = false;

        bus.Use((_, _) => Task.CompletedTask); // Does not call next()

        bus.Subscribe<TestEvent>((_, _) => { handlerCalled = true; return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Use_MiddlewareCanPassDataViaItems()
    {
        var bus = new EventBus();
        object? capturedValue = null;

        bus.Use(async (context, next) =>
        {
            context.Items["traceId"] = "abc-123";
            await next();
        });

        bus.Use(async (context, next) =>
        {
            capturedValue = context.Items["traceId"];
            await next();
        });

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal("abc-123", capturedValue);
    }

    [Fact]
    public async Task Use_WithNoMiddleware_HandlerStillExecutes()
    {
        var bus = new EventBus();
        var handlerCalled = false;

        bus.Subscribe<TestEvent>((_, _) => { handlerCalled = true; return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.True(handlerCalled);
    }

    [Fact]
    public void Use_WithNullMiddleware_ThrowsArgumentNullException()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Use(null!));
    }

    [Fact]
    public async Task Use_MiddlewareExceptionPropagatesWithThrowOnHandlerError()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnHandlerError = true });

        bus.Use((_, _) => throw new InvalidOperationException("middleware error"));

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new TestEvent("test")));
    }

    [Fact]
    public async Task Use_MiddlewareExceptionSwallowedWithoutThrowOnHandlerError()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnHandlerError = false });

        bus.Use((_, _) => throw new InvalidOperationException("middleware error"));

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("test"));
    }

    [Fact]
    public async Task Use_MiddlewareAppliedToEachHandler()
    {
        var bus = new EventBus();
        var middlewareCallCount = 0;

        bus.Use(async (_, next) =>
        {
            middlewareCallCount++;
            await next();
        });

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(2, middlewareCallCount);
    }
}
