using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Controllers;
using Server.Infrastructure.Database;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;

namespace Server.Tests.Controllers;

public class InvoicesControllerTests
{
    [Fact]
    public async Task CreateInvoice_WhenSuccessful_ReturnsOk()
    {
        var service = new Mock<IInvoiceService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<InvoicesController>>();
        var request = new CreateInvoiceRequest("100", 10m, DateTime.UtcNow, "Open");
        var expected = new Invoice("inv", request.CustomerId, request.Amount, request.IssuedOn, request.Status);
        service.Setup(s => s.CreateInvoiceAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var controller = new InvoicesController(service.Object, database.Object, logger);

        var result = await controller.CreateInvoice(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Once);
        database.Verify(d => d.RollbackTran(), Times.Never);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateInvoice_WhenDomainException_ReturnsBadRequest()
    {
        var service = new Mock<IInvoiceService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<InvoicesController>>();
        var request = new CreateInvoiceRequest("100", 10m, DateTime.UtcNow, "Open");
        service.Setup(s => s.CreateInvoiceAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("failure"));

        var controller = new InvoicesController(service.Object, database.Object, logger);

        var result = await controller.CreateInvoice(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
