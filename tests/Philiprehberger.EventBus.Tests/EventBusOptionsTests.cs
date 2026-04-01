using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class EventBusOptionsTests
{
    [Fact]
    public void Defaults_ThrowOnHandlerErrorIsFalse()
    {
        var options = new EventBusOptions();

        Assert.False(options.ThrowOnHandlerError);
    }

    [Fact]
    public void Defaults_MaxConcurrencyIsZero()
    {
        var options = new EventBusOptions();

        Assert.Equal(0, options.MaxConcurrency);
    }

    [Fact]
    public void Defaults_OnHandlerErrorIsNull()
    {
        var options = new EventBusOptions();

        Assert.Null(options.OnHandlerError);
    }

    [Fact]
    public void Defaults_HandlerTimeoutIsNull()
    {
        var options = new EventBusOptions();

        Assert.Null(options.HandlerTimeout);
    }

    [Fact]
    public void Defaults_OnDeadLetterIsNull()
    {
        var options = new EventBusOptions();

        Assert.Null(options.OnDeadLetter);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsProperties()
    {
        var errorHandler = (Exception _) => { };
        var deadLetterHandler = (object _, Exception _) => { };
        var timeout = TimeSpan.FromSeconds(10);

        var options = new EventBusOptions
        {
            ThrowOnHandlerError = true,
            MaxConcurrency = 5,
            OnHandlerError = errorHandler,
            HandlerTimeout = timeout,
            OnDeadLetter = deadLetterHandler
        };

        Assert.True(options.ThrowOnHandlerError);
        Assert.Equal(5, options.MaxConcurrency);
        Assert.Same(errorHandler, options.OnHandlerError);
        Assert.Equal(timeout, options.HandlerTimeout);
        Assert.Same(deadLetterHandler, options.OnDeadLetter);
    }
}
