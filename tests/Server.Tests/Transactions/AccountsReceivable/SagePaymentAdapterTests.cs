using FluentAssertions;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Sdk;

namespace Server.Tests.Transactions.AccountsReceivable;

public class SagePaymentAdapterTests
{
    [Fact]
    public async Task ApplyPaymentAsync_MapsSageResponse()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        var paidOn = new DateTime(2024, 1, 2);
        var sdkPayment = new SagePayment("PAY-100", "INV-10", 75m, paidOn, "Posted");
        SagePaymentDraft? capturedDraft = null;
        client.Setup(c => c.ApplyPaymentAsync(It.IsAny<SagePaymentDraft>(), It.IsAny<CancellationToken>()))
            .Callback<SagePaymentDraft, CancellationToken>((draft, _) => capturedDraft = draft)
            .ReturnsAsync(sdkPayment);

        var adapter = new SagePaymentAdapter(client.Object);
        var payment = new Payment("TEMP", "INV-10", 75m, paidOn);

        var result = await adapter.ApplyPaymentAsync(payment, CancellationToken.None);

        capturedDraft.Should().NotBeNull();
        capturedDraft!.InvoiceDocumentNumber.Should().Be("INV-10");
        capturedDraft.Amount.Should().Be(75m);
        capturedDraft.PaidOn.Should().Be(paidOn);
        capturedDraft.ExternalReference.Should().Be("TEMP");

        result.Should().Be(new Payment("PAY-100", "INV-10", 75m, paidOn));
    }

    [Fact]
    public async Task ApplyPaymentAsync_WhenInvoiceMissing_ThrowsNotFound()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        client.Setup(c => c.ApplyPaymentAsync(It.IsAny<SagePaymentDraft>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageEntityNotFoundException("invoice missing"));

        var adapter = new SagePaymentAdapter(client.Object);
        var payment = new Payment("TEMP", "INV-10", 75m, DateTime.UtcNow);

        var act = async () => await adapter.ApplyPaymentAsync(payment, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ApplyPaymentAsync_WhenSdkFails_ThrowsDomainException()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        client.Setup(c => c.ApplyPaymentAsync(It.IsAny<SagePaymentDraft>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageSdkException("boom"));

        var adapter = new SagePaymentAdapter(client.Object);
        var payment = new Payment("TEMP", "INV-10", 75m, DateTime.UtcNow);

        var act = async () => await adapter.ApplyPaymentAsync(payment, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
