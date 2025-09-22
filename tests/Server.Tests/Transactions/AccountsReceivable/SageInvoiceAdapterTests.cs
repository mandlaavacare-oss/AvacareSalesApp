using FluentAssertions;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Sdk;

namespace Server.Tests.Transactions.AccountsReceivable;

public class SageInvoiceAdapterTests
{
    [Fact]
    public async Task CreateInvoiceAsync_MapsSageResponse()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        var issuedOn = new DateTime(2024, 1, 1);
        var sdkInvoice = new SageInvoice("INV-100", "100", 150m, issuedOn, "Posted");
        SageInvoiceDraft? capturedDraft = null;
        client.Setup(c => c.CreateInvoiceAsync(It.IsAny<SageInvoiceDraft>(), It.IsAny<CancellationToken>()))
            .Callback<SageInvoiceDraft, CancellationToken>((draft, _) => capturedDraft = draft)
            .ReturnsAsync(sdkInvoice);

        var adapter = new SageInvoiceAdapter(client.Object);
        var invoice = new Invoice("TEMP", "100", 150m, issuedOn, "Open");

        var result = await adapter.CreateInvoiceAsync(invoice, CancellationToken.None);

        capturedDraft.Should().NotBeNull();
        capturedDraft!.CustomerCode.Should().Be("100");
        capturedDraft.Amount.Should().Be(150m);
        capturedDraft.IssuedOn.Should().Be(issuedOn);
        capturedDraft.Status.Should().Be("Open");
        capturedDraft.ExternalReference.Should().Be("TEMP");

        result.Should().Be(new Invoice("INV-100", "100", 150m, issuedOn, "Posted"));
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenCustomerMissing_ThrowsNotFound()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        client.Setup(c => c.CreateInvoiceAsync(It.IsAny<SageInvoiceDraft>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageEntityNotFoundException("customer missing"));

        var adapter = new SageInvoiceAdapter(client.Object);
        var invoice = new Invoice("TEMP", "missing", 10m, DateTime.UtcNow, "Open");

        var act = async () => await adapter.CreateInvoiceAsync(invoice, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenSdkFails_ThrowsDomainException()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        client.Setup(c => c.CreateInvoiceAsync(It.IsAny<SageInvoiceDraft>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageSdkException("boom"));

        var adapter = new SageInvoiceAdapter(client.Object);
        var invoice = new Invoice("TEMP", "100", 10m, DateTime.UtcNow, "Open");

        var act = async () => await adapter.CreateInvoiceAsync(invoice, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
