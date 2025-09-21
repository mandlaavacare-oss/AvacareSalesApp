using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Controllers;
using Server.Infrastructure.Database;
using Server.Transactions.OrderEntry.Models;
using Server.Transactions.OrderEntry.Services;

namespace Server.Tests.Controllers;

public class OrdersControllerTests
{
    [Fact]
    public async Task CreateOrder_WhenSuccessful_ReturnsOk()
    {
        var service = new Mock<IOrderService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<OrdersController>>();
        var request = new CreateOrderRequest("cust-1", DateTime.UtcNow, new[] { new SalesOrderLine("sku", 1, 10m) });
        var expected = new SalesOrder("order-1", request.CustomerId, request.OrderDate, request.Lines);
        service.Setup(s => s.CreateOrderAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var controller = new OrdersController(service.Object, database.Object, logger);

        var result = await controller.CreateOrder(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Once);
        database.Verify(d => d.RollbackTran(), Times.Never);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateOrder_WhenDomainException_ReturnsBadRequest()
    {
        var service = new Mock<IOrderService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<OrdersController>>();
        var request = new CreateOrderRequest("cust-1", DateTime.UtcNow, new[] { new SalesOrderLine("sku", 1, 10m) });
        service.Setup(s => s.CreateOrderAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("failure"));

        var controller = new OrdersController(service.Object, database.Object, logger);

        var result = await controller.CreateOrder(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
