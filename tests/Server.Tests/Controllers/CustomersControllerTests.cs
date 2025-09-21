using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Controllers;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Database;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;

namespace Server.Tests.Controllers;

public class CustomersControllerTests
{
    [Fact]
    public async Task GetCustomer_WhenFound_ReturnsOk()
    {
        var service = new Mock<ICustomerService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<CustomersController>>();
        var customer = new Customer("100", "Acme", "info@acme.test", 1000m);
        service.Setup(s => s.GetCustomerAsync("100", It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var controller = new CustomersController(service.Object, database.Object, logger);

        var result = await controller.GetCustomer("100", CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Once);
        database.Verify(d => d.RollbackTran(), Times.Never);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(customer);
    }

    [Fact]
    public async Task GetCustomer_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<ICustomerService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<CustomersController>>();
        service.Setup(s => s.GetCustomerAsync("missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("missing"));

        var controller = new CustomersController(service.Object, database.Object, logger);

        var result = await controller.GetCustomer("missing", CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCustomer_WhenDomainException_ReturnsBadRequest()
    {
        var service = new Mock<ICustomerService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<CustomersController>>();
        service.Setup(s => s.GetCustomerAsync("100", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("failure"));

        var controller = new CustomersController(service.Object, database.Object, logger);

        var result = await controller.GetCustomer("100", CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetCustomer_WhenCustomerAccessesOwnRecord_ReturnsOk()
    {
        var service = new Mock<ICustomerService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<CustomersController>>();
        var customer = new Customer("100", "Acme", "info@acme.test", 1000m);
        service.Setup(s => s.GetCustomerAsync("100", It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var controller = new CustomersController(service.Object, database.Object, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.Name, "customer"),
                            new Claim(ClaimTypes.Role, RoleNames.Customer),
                            new Claim(CustomClaimTypes.CustomerId, "100")
                        },
                        "TestAuth"))
                }
            }
        };

        var result = await controller.GetCustomer("100", CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Once);
        database.Verify(d => d.RollbackTran(), Times.Never);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCustomer_WhenCustomerAccessesAnotherRecord_ReturnsForbid()
    {
        var service = new Mock<ICustomerService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<CustomersController>>();

        var controller = new CustomersController(service.Object, database.Object, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.Name, "customer"),
                            new Claim(ClaimTypes.Role, RoleNames.Customer),
                            new Claim(CustomClaimTypes.CustomerId, "200")
                        },
                        "TestAuth"))
                }
            }
        };

        var result = await controller.GetCustomer("100", CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<ForbidResult>();
        service.Verify(s => s.GetCustomerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
