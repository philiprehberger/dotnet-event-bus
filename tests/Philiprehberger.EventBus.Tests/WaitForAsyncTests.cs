using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class WaitForAsyncTests
{
    private record TestEvent(string Message);

    [Fact]
    public async Task WaitForAsync_CompletesOnNextEvent()
    {
        var bus = new EventBus();

        var waitTask = bus.WaitForAsync<TestEvent>();
        await bus.PublishAsync(new TestEvent("hello"));

        var result = await waitTask;
        Assert.Equal("hello", result.Message);
    }

    [Fact]
    public async Task WaitForAsync_WithFilter_SkipsNonMatching()
    {
        var bus = new EventBus();

        var waitTask = bus.WaitForAsync<TestEvent>(filter: e => e.Message == "target");
        await bus.PublishAsync(new TestEvent("ignore"));
        await bus.PublishAsync(new TestEvent("target"));

        var result = await waitTask;
        Assert.Equal("target", result.Message);
    }

    [Fact]
    public async Task WaitForAsync_CancellationToken_CancelsTask()
    {
        var bus = new EventBus();
        using var cts = new CancellationTokenSource();

        var waitTask = bus.WaitForAsync<TestEvent>(ct: cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => waitTask);
    }

    [Fact]
    public async Task WaitForAsync_MultipleConcurrentWaiters()
    {
        var bus = new EventBus();

        var wait1 = bus.WaitForAsync<TestEvent>();
        var wait2 = bus.WaitForAsync<TestEvent>();

        await bus.PublishAsync(new TestEvent("hello"));

        var result1 = await wait1;
        var result2 = await wait2;

        Assert.Equal("hello", result1.Message);
        Assert.Equal("hello", result2.Message);
    }

    [Fact]
    public async Task WaitForAsync_DoesNotCaptureSubsequentEvents()
    {
        var bus = new EventBus();

        var waitTask = bus.WaitForAsync<TestEvent>();
        await bus.PublishAsync(new TestEvent("first"));
        await bus.PublishAsync(new TestEvent("second"));

        var result = await waitTask;
        Assert.Equal("first", result.Message);
    }
}
