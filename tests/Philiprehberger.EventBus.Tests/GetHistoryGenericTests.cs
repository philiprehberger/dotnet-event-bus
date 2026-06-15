using Xunit;
using Philiprehberger.EventBus;

namespace Philiprehberger.EventBus.Tests;

public class GetHistoryGenericTests
{
    private record OrderPlaced(int OrderId);
    private record UserRegistered(string Email);

    [Fact]
    public async Task GetHistoryT_FiltersToRequestedType()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        await bus.PublishAsync(new OrderPlaced(1));
        await bus.PublishAsync(new UserRegistered("a@example.com"));
        await bus.PublishAsync(new OrderPlaced(2));

        var orders = bus.GetHistory<OrderPlaced>();
        var users = bus.GetHistory<UserRegistered>();

        Assert.Equal(2, orders.Count);
        Assert.Equal(new OrderPlaced(1), orders[0]);
        Assert.Equal(new OrderPlaced(2), orders[1]);

        Assert.Single(users);
        Assert.Equal(new UserRegistered("a@example.com"), users[0]);
    }

    [Fact]
    public async Task GetHistoryT_NoMatches_ReturnsEmpty()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);
        await bus.PublishAsync(new OrderPlaced(1));

        var users = bus.GetHistory<UserRegistered>();
        Assert.Empty(users);
    }

    [Fact]
    public void GetHistoryT_WithoutEnableHistory_Throws()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.GetHistory<OrderPlaced>());
    }

    [Fact]
    public async Task GetHistoryT_PreservesChronologicalOrder()
    {
        var bus = new EventBus();
        bus.EnableHistory(10);

        for (var i = 1; i <= 5; i++)
            await bus.PublishAsync(new OrderPlaced(i));

        var orders = bus.GetHistory<OrderPlaced>();
        Assert.Equal(5, orders.Count);
        for (var i = 0; i < 5; i++)
            Assert.Equal(i + 1, orders[i].OrderId);
    }
}
