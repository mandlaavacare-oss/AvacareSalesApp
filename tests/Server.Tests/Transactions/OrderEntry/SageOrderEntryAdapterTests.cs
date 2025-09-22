using FluentAssertions;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.OrderEntry.Adapters;
using Server.Transactions.OrderEntry.Models;
using Server.Transactions.OrderEntry.Sdk;

namespace Server.Tests.Transactions.OrderEntry;

public class SageOrderEntryAdapterTests
{
    [Fact]
    public async Task CreateOrderAsync_ReturnsOrderFromSdkResponse()
    {
        var sdk = new Mock<ISageOrderEntrySdk>();
        var order = new SalesOrder("temp-id", "cust-1", new DateTime(2024, 1, 1),
            new List<SalesOrderLine> { new("sku-1", 2, 15m) }, "Pending");
        var responseLines = new List<SageOrderLine> { new("sku-1", 2, 14.5m) };
        var response = new SageOrderResponse("SO123", SageOrderStatus.Released, responseLines);

        sdk.Setup(s => s.CreateOrderAsync(It.IsAny<SageOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var adapter = new SageOrderEntryAdapter(sdk.Object);

        var result = await adapter.CreateOrderAsync(order, CancellationToken.None);

        result.Should().BeEquivalentTo(new SalesOrder("SO123", order.CustomerId, order.OrderDate,
            new List<SalesOrderLine> { new("sku-1", 2, 14.5m) }, "Released"));

        sdk.Verify(s => s.CreateOrderAsync(It.Is<SageOrderRequest>(r =>
            r.CustomerId == order.CustomerId &&
            r.OrderDate == order.OrderDate &&
            r.Lines.Single().ProductCode == "sku-1" &&
            r.Lines.Single().Quantity == 2 &&
            r.Lines.Single().UnitPrice == 15m), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateOrderAsync_WhenSdkThrows_ThrowsDomainException()
    {
        var sdk = new Mock<ISageOrderEntrySdk>();
        var order = new SalesOrder("temp", "cust-1", DateTime.UtcNow,
            new List<SalesOrderLine> { new("sku", 1, 10m) }, "Pending");

        sdk.Setup(s => s.CreateOrderAsync(It.IsAny<SageOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("failure"));

        var adapter = new SageOrderEntryAdapter(sdk.Object);

        var act = async () => await adapter.CreateOrderAsync(order, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Unable to create order in Sage.*");
    }

    [Fact]
    public async Task CreateOrderAsync_WhenSdkReturnsNull_ThrowsDomainException()
    {
        var sdk = new Mock<ISageOrderEntrySdk>();
        var order = new SalesOrder("temp", "cust-1", DateTime.UtcNow,
            new List<SalesOrderLine> { new("sku", 1, 10m) }, "Pending");

        sdk.Setup(s => s.CreateOrderAsync(It.IsAny<SageOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SageOrderResponse?)null);

        var adapter = new SageOrderEntryAdapter(sdk.Object);

        var act = async () => await adapter.CreateOrderAsync(order, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Sage returned no order response.");
    }

    [Fact]
    public async Task CreateOrderAsync_WhenStatusUnknown_ReturnsUnknownStatus()
    {
        var sdk = new Mock<ISageOrderEntrySdk>();
        var order = new SalesOrder("temp", "cust-1", DateTime.UtcNow,
            new List<SalesOrderLine> { new("sku", 1, 10m) }, "Pending");
        var response = new SageOrderResponse("SO999", (SageOrderStatus)999, new List<SageOrderLine>());

        sdk.Setup(s => s.CreateOrderAsync(It.IsAny<SageOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var adapter = new SageOrderEntryAdapter(sdk.Object);

        var result = await adapter.CreateOrderAsync(order, CancellationToken.None);

        result.Status.Should().Be("Unknown");
    }
}
