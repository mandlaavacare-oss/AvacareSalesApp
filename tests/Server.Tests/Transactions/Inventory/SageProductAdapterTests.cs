using FluentAssertions;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.Inventory.Adapters;
using Server.Transactions.Inventory.Models;
using Server.Transactions.Inventory.Sdk;

namespace Server.Tests.Transactions.Inventory;

public class SageProductAdapterTests
{
    [Fact]
    public async Task GetProductsAsync_MapsProductsFromSdk()
    {
        var sdk = new Mock<ISageInventorySdk>();
        var items = new List<SageInventoryItem>
        {
            new("sku-1", "Widget", "Standard widget", 10.5m, 7),
            new("sku-2", "Gadget", "High end gadget", 25m, 2)
        };

        sdk.Setup(s => s.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var adapter = new SageProductAdapter(sdk.Object);

        var products = await adapter.GetProductsAsync(CancellationToken.None);

        products.Should().BeEquivalentTo(new List<Product>
        {
            new("sku-1", "Widget", "Standard widget", 10.5m, 7),
            new("sku-2", "Gadget", "High end gadget", 25m, 2)
        });
    }

    [Fact]
    public async Task GetProductsAsync_WhenSdkThrows_ThrowsDomainException()
    {
        var sdk = new Mock<ISageInventorySdk>();
        sdk.Setup(s => s.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var adapter = new SageProductAdapter(sdk.Object);

        var act = async () => await adapter.GetProductsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Unable to retrieve products from Sage.*");
    }

    [Fact]
    public async Task GetProductsAsync_WhenSdkReturnsNull_ThrowsDomainException()
    {
        var sdk = new Mock<ISageInventorySdk>();
        sdk.Setup(s => s.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<SageInventoryItem>?)null);

        var adapter = new SageProductAdapter(sdk.Object);

        var act = async () => await adapter.GetProductsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Sage returned an empty product list.");
    }
}
