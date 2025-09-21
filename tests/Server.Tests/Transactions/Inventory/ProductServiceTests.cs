using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.Inventory.Adapters;
using Server.Transactions.Inventory.Models;
using Server.Transactions.Inventory.Services;

namespace Server.Tests.Transactions.Inventory;

public class ProductServiceTests
{
    [Fact]
    public async Task GetProductsAsync_ReturnsProducts()
    {
        var adapter = new Mock<IProductAdapter>();
        var logger = Mock.Of<ILogger<ProductService>>();
        var expected = new List<Product> { new("sku-1", "Widget", "", 10m, 5) };
        adapter.Setup(a => a.GetProductsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var service = new ProductService(adapter.Object, logger);

        var products = await service.GetProductsAsync(CancellationToken.None);

        products.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetProductsAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<IProductAdapter>();
        var logger = new Mock<ILogger<ProductService>>();
        adapter.Setup(a => a.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var service = new ProductService(adapter.Object, logger.Object);

        var act = async () => await service.GetProductsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
