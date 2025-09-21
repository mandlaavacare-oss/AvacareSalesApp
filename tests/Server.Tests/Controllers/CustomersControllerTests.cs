using FluentAssertions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Controllers;
using Server.Infrastructure.Authentication;
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

        var controller = CreateController(service.Object, database.Object, logger, RoleNames.Admin);

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

        var controller = CreateController(service.Object, database.Object, logger, RoleNames.Admin);

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

        var controller = CreateController(service.Object, database.Object, logger, RoleNames.Admin);

        var result = await controller.GetCustomer("100", CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetCustomer_WhenCustomerAccessingOtherAccount_ReturnsForbid()
    {
        var service = new Mock<ICustomerService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<CustomersController>>();

        var controller = CreateController(service.Object, database.Object, logger, RoleNames.Customer, customerId: "CUST-0001");

        var result = await controller.GetCustomer("CUST-9999", CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Never);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    private static CustomersController CreateController(
        ICustomerService service,
        IDatabaseContext database,
        ILogger<CustomersController> logger,
        string role,
        string? customerId = null)
    {
        var controller = new CustomersController(service, database, logger);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, role)
        };

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            claims.Add(new Claim(IdentityClaimTypes.CustomerId, customerId));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }
}
