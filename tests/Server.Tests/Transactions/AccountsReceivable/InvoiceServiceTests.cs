using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;

namespace Server.Tests.Transactions.AccountsReceivable;

public class InvoiceServiceTests
{
    [Fact]
    public async Task CreateInvoiceAsync_DelegatesToAdapter()
    {
        var adapter = new Mock<IInvoiceAdapter>();
        var logger = Mock.Of<ILogger<InvoiceService>>();
        var request = new CreateInvoiceRequest("100", 150m, new DateTime(2024, 1, 1), "Open");
        var expected = new Invoice("inv-1", request.CustomerId, request.Amount, request.IssuedOn, request.Status);

        adapter.Setup(a => a.CreateInvoiceAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new InvoiceService(adapter.Object, logger);

        var invoice = await service.CreateInvoiceAsync(request, CancellationToken.None);

        invoice.Should().Be(expected);
        adapter.Verify(a => a.CreateInvoiceAsync(It.Is<Invoice>(i =>
            i.CustomerId == request.CustomerId &&
            i.Amount == request.Amount &&
            i.IssuedOn == request.IssuedOn &&
            i.Status == request.Status), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<IInvoiceAdapter>();
        var logger = new Mock<ILogger<InvoiceService>>();
        var request = new CreateInvoiceRequest("100", 150m, DateTime.UtcNow, "Open");

        adapter.Setup(a => a.CreateInvoiceAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var service = new InvoiceService(adapter.Object, logger.Object);

        var act = async () => await service.CreateInvoiceAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
