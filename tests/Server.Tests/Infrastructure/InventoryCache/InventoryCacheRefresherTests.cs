using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Server.Infrastructure.InventoryCache;
using Server.Transactions.Inventory.Adapters;

namespace Server.Tests.Infrastructure.InventoryCache;

public class InventoryCacheRefresherTests
{
    [Fact]
    public async Task RefreshAsync_ReplacesInventoryWithAdapterResults()
    {
        var adapter = new Mock<IProductAdapter>();
        var repository = new Mock<ICacheInventoryRepository>();
        var logger = Mock.Of<ILogger<InventoryCacheRefresher>>();
        var options = Options.Create(new InventoryCacheOptions { BatchSize = 25 });
        var cacheItems = new List<CacheInventoryItem>
        {
            new()
            {
                Sku = "SKU-1",
                Name = "Widget",
                Description = string.Empty,
                Price = 10m,
                QuantityOnHand = 5,
                SyncedAt = DateTimeOffset.UtcNow,
            },
        };

        adapter.Setup(a => a.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cacheItems);

        var refresher = new InventoryCacheRefresher(adapter.Object, repository.Object, options, logger);

        await refresher.RefreshAsync(CancellationToken.None);

        repository.Verify(r => r.ReplaceInventoryAsync(cacheItems, 25, It.IsAny<CancellationToken>()), Times.Once);
    }
}
