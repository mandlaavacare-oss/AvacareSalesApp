using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Server.Common.Exceptions;
using Server.Infrastructure.InventoryCache;
using Server.Transactions.Inventory.Models;
using Server.Transactions.Inventory.Services;

namespace Server.Tests.Transactions.Inventory;

public class ProductServiceTests
{
    [Fact]
    public async Task GetProductsAsync_ReturnsProducts()
    {
        var repository = new Mock<ICacheInventoryRepository>();
        var logger = Mock.Of<ILogger<ProductService>>();
        var now = DateTimeOffset.UtcNow;
        var options = Options.Create(new InventoryCacheOptions { StaleAfter = TimeSpan.FromHours(1) });
        var timeProvider = new TestTimeProvider(now);
        var cachedItems = new List<CacheInventoryItem>
        {
            new()
            {
                Sku = "sku-1",
                Name = "Widget",
                Description = "",
                Price = 10m,
                QuantityOnHand = 5,
                SyncedAt = now,
            },
        };

        repository.Setup(r => r.GetItemsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cachedItems);
        repository.Setup(r => r.GetLastSyncedAtAsync(It.IsAny<CancellationToken>())).ReturnsAsync(now);

        var service = new ProductService(repository.Object, options, logger, timeProvider);

        var products = await service.GetProductsAsync(CancellationToken.None);

        products.Should().BeEquivalentTo(new List<Product> { new("sku-1", "Widget", "", 10m, 5) });
    }

    [Fact]
    public async Task GetProductsAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var repository = new Mock<ICacheInventoryRepository>();
        var logger = new Mock<ILogger<ProductService>>();
        var options = Options.Create(new InventoryCacheOptions { StaleAfter = null });
        repository.Setup(r => r.GetItemsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var service = new ProductService(repository.Object, options, logger.Object);

        var act = async () => await service.GetProductsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task GetProductsAsync_WhenCacheIsStale_ThrowsDomainException()
    {
        var repository = new Mock<ICacheInventoryRepository>();
        var logger = Mock.Of<ILogger<ProductService>>();
        var now = DateTimeOffset.UtcNow;
        var options = Options.Create(new InventoryCacheOptions { StaleAfter = TimeSpan.FromMinutes(5) });
        var timeProvider = new TestTimeProvider(now);

        repository.Setup(r => r.GetItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CacheInventoryItem>
            {
                new()
                {
                    Sku = "sku-1",
                    Name = "Widget",
                    Description = string.Empty,
                    Price = 10m,
                    QuantityOnHand = 5,
                    SyncedAt = now.AddHours(-1),
                },
            });

        repository.Setup(r => r.GetLastSyncedAtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(now.AddHours(-1));

        var service = new ProductService(repository.Object, options, logger, timeProvider);

        var act = async () => await service.GetProductsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task GetProductsAsync_WhenCacheEmpty_ThrowsDomainException()
    {
        var repository = new Mock<ICacheInventoryRepository>();
        var logger = Mock.Of<ILogger<ProductService>>();
        var options = Options.Create(new InventoryCacheOptions { StaleAfter = null });

        repository.Setup(r => r.GetItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CacheInventoryItem>());

        var service = new ProductService(repository.Object, options, logger);

        var act = async () => await service.GetProductsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

internal sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public TestTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }
}
