using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.OrderEntry.Adapters;
using Server.Transactions.OrderEntry.Models;
using Server.Transactions.OrderEntry.Services;

namespace Server.Tests.Transactions.OrderEntry;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_DelegatesToAdapter()
    {
        var adapter = new Mock<IOrderEntryAdapter>();
        var logger = Mock.Of<ILogger<OrderService>>();
        var lines = new List<SalesOrderLine> { new("sku-1", 2, 10m) };
        var request = new CreateOrderRequest("cust-1", new DateTime(2024, 1, 1), lines);
        var expected = new SalesOrder("order-1", request.CustomerId, request.OrderDate, lines);

        adapter.Setup(a => a.CreateOrderAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new OrderService(adapter.Object, logger);

        var order = await service.CreateOrderAsync(request, CancellationToken.None);

        order.Should().Be(expected);
        adapter.Verify(a => a.CreateOrderAsync(It.Is<SalesOrder>(o =>
            o.CustomerId == request.CustomerId &&
            o.OrderDate == request.OrderDate &&
            o.Lines.SequenceEqual(request.Lines)), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateOrderAsync_WhenNoLines_ThrowsDomainException()
    {
        var adapter = new Mock<IOrderEntryAdapter>();
        var logger = Mock.Of<ILogger<OrderService>>();
        var request = new CreateOrderRequest("cust-1", DateTime.UtcNow, Array.Empty<SalesOrderLine>());

        var service = new OrderService(adapter.Object, logger);

        var act = async () => await service.CreateOrderAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task CreateOrderAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<IOrderEntryAdapter>();
        var logger = new Mock<ILogger<OrderService>>();
        var request = new CreateOrderRequest("cust-1", DateTime.UtcNow, new[] { new SalesOrderLine("sku", 1, 1m) });

        adapter.Setup(a => a.CreateOrderAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var service = new OrderService(adapter.Object, logger.Object);

        var act = async () => await service.CreateOrderAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
