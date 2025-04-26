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
    public void Constructor_WithCustomValues_SetsProperties()
    {
        var options = new EventBusOptions(ThrowOnHandlerError: true, MaxConcurrency: 5);

        Assert.True(options.ThrowOnHandlerError);
        Assert.Equal(5, options.MaxConcurrency);
    }
}
