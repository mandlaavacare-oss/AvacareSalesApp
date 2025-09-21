using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Infrastructure.InventoryCache;
using Server.Transactions.Inventory.Adapters;

namespace Server.Tests.Transactions.Inventory;

public class SageProductAdapterTests
{
    [Fact]
    public async Task GetProductsAsync_MapsSdkProductsToCacheItems()
    {
        var client = new Mock<ISageInventoryClient>();
        var logger = Mock.Of<ILogger<SageProductAdapter>>();
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new TestTimeProvider(now);
        var sdkProducts = new List<SageInventoryProduct>
        {
            new("SKU-1", "Widget", "Widget description", 10m, 5),
            new("SKU-2", "Gadget", "Gadget description", 20m, 2),
        };

        client.Setup(c => c.GetInventoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sdkProducts);

        var adapter = new SageProductAdapter(client.Object, logger, timeProvider);

        var result = await adapter.GetProductsAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(new CacheInventoryItem
        {
            Sku = "SKU-1",
            Name = "Widget",
            Description = "Widget description",
            Price = 10m,
            QuantityOnHand = 5,
            SyncedAt = now,
        });
    }
}
