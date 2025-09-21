using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Controllers;
using Server.Infrastructure.Database;
using Server.Transactions.Inventory.Models;
using Server.Transactions.Inventory.Services;

namespace Server.Tests.Controllers;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetProducts_WhenSuccessful_ReturnsOk()
    {
        var service = new Mock<IProductService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<ProductsController>>();
        var products = new List<Product> { new("sku-1", "Widget", "", 10m, 5) };
        service.Setup(s => s.GetProductsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var controller = new ProductsController(service.Object, database.Object, logger);

        var result = await controller.GetProducts(CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Once);
        database.Verify(d => d.RollbackTran(), Times.Never);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(products);
    }

    [Fact]
    public async Task GetProducts_WhenDomainException_ReturnsBadRequest()
    {
        var service = new Mock<IProductService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<ProductsController>>();
        service.Setup(s => s.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("failure"));

        var controller = new ProductsController(service.Object, database.Object, logger);

        var result = await controller.GetProducts(CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
