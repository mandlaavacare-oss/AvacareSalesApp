using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;
using Server.Transactions.AccountsReceivable.Sdk;

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

    [Fact]
    public async Task ApplyPaymentAsync_WithSageAdapter_MapsIdentifiers()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        var logger = Mock.Of<ILogger<PaymentService>>();
        var request = new ApplyPaymentRequest("inv-1", 120m, new DateTime(2024, 1, 2));
        SagePaymentDraft? capturedDraft = null;
        client.Setup(c => c.ApplyPaymentAsync(It.IsAny<SagePaymentDraft>(), It.IsAny<CancellationToken>()))
            .Callback<SagePaymentDraft, CancellationToken>((draft, _) => capturedDraft = draft)
            .ReturnsAsync(new SagePayment("PAY-10", "inv-1", 120m, request.PaidOn, "Posted"));

        var adapter = new SagePaymentAdapter(client.Object);
        var service = new PaymentService(adapter, logger);

        var payment = await service.ApplyPaymentAsync(request, CancellationToken.None);

        capturedDraft.Should().NotBeNull();
        capturedDraft!.InvoiceDocumentNumber.Should().Be(request.InvoiceId);
        capturedDraft.Amount.Should().Be(request.Amount);
        capturedDraft.PaidOn.Should().Be(request.PaidOn);
        capturedDraft.ExternalReference.Should().NotBeNullOrWhiteSpace();

        payment.Should().Be(new Payment("PAY-10", "inv-1", 120m, request.PaidOn));
    }
}
