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

public class PaymentsControllerTests
{
    [Fact]
    public async Task ApplyPayment_WhenSuccessful_ReturnsOk()
    {
        var service = new Mock<IPaymentService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<PaymentsController>>();
        var request = new ApplyPaymentRequest("inv", 10m, DateTime.UtcNow);
        var expected = new Payment("pay", request.InvoiceId, request.Amount, request.PaidOn);
        service.Setup(s => s.ApplyPaymentAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var controller = new PaymentsController(service.Object, database.Object, logger);

        var result = await controller.ApplyPayment(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Once);
        database.Verify(d => d.RollbackTran(), Times.Never);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ApplyPayment_WhenDomainException_ReturnsBadRequest()
    {
        var service = new Mock<IPaymentService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<PaymentsController>>();
        var request = new ApplyPaymentRequest("inv", 10m, DateTime.UtcNow);
        service.Setup(s => s.ApplyPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("failure"));

        var controller = new PaymentsController(service.Object, database.Object, logger);

        var result = await controller.ApplyPayment(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
