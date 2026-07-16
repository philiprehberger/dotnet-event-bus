using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class ClearMiddlewareTests
{
    private record TestEvent(string Message);

    [Fact]
    public async Task ClearMiddleware_RemovesAllRegisteredMiddleware()
    {
        var bus = new EventBus();
        var log = new List<string>();

        bus.Use(async (_, next) => { log.Add("mw"); await next(); });
        bus.ClearMiddleware();

        bus.Subscribe<TestEvent>((_, _) => { log.Add("handler"); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(new[] { "handler" }, log);
    }

    [Fact]
    public async Task ClearMiddleware_AllowsReRegistration()
    {
        var bus = new EventBus();
        var log = new List<string>();

        bus.Use(async (_, next) => { log.Add("first"); await next(); });
        bus.ClearMiddleware();
        bus.Use(async (_, next) => { log.Add("second"); await next(); });

        bus.Subscribe<TestEvent>((_, _) => Task.CompletedTask);

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(new[] { "second" }, log);
    }

    [Fact]
    public void ClearMiddleware_WithNoMiddleware_DoesNotThrow()
    {
        var bus = new EventBus();
        bus.ClearMiddleware();
    }
}
