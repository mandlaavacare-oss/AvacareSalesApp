using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;

namespace Server.Tests.Transactions.AccountsReceivable;

public class PaymentServiceTests
{
    [Fact]
    public async Task ApplyPaymentAsync_DelegatesToAdapter()
    {
        var adapter = new Mock<IPaymentAdapter>();
        var logger = Mock.Of<ILogger<PaymentService>>();
        var request = new ApplyPaymentRequest("inv-1", 120m, new DateTime(2024, 1, 2));
        var expected = new Payment("pay-1", request.InvoiceId, request.Amount, request.PaidOn);

        adapter.Setup(a => a.ApplyPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new PaymentService(adapter.Object, logger);

        var payment = await service.ApplyPaymentAsync(request, CancellationToken.None);

        payment.Should().Be(expected);
        adapter.Verify(a => a.ApplyPaymentAsync(It.Is<Payment>(p =>
            p.InvoiceId == request.InvoiceId &&
            p.Amount == request.Amount &&
            p.PaidOn == request.PaidOn), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ApplyPaymentAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<IPaymentAdapter>();
        var logger = new Mock<ILogger<PaymentService>>();
        var request = new ApplyPaymentRequest("inv-1", 120m, DateTime.UtcNow);

        adapter.Setup(a => a.ApplyPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var service = new PaymentService(adapter.Object, logger.Object);

        var act = async () => await service.ApplyPaymentAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
