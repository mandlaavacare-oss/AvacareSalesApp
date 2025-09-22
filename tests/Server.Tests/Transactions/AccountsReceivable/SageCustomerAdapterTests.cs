using FluentAssertions;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Sdk;

namespace Server.Tests.Transactions.AccountsReceivable;

public class SageCustomerAdapterTests
{
    [Fact]
    public async Task GetCustomerAsync_MapsSageDtoToDomain()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        var sdkCustomer = new SageCustomer("100", "Acme Corp", "info@acme.test", 1200m);
        client.Setup(c => c.GetCustomerAsync("100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sdkCustomer);

        var adapter = new SageCustomerAdapter(client.Object);

        var customer = await adapter.GetCustomerAsync("100", CancellationToken.None);

        customer.Should().Be(new Customer("100", "Acme Corp", "info@acme.test", 1200m));
    }

    [Fact]
    public async Task GetCustomerAsync_WhenSdkReturnsNull_ReturnsNull()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        client.Setup(c => c.GetCustomerAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SageCustomer?)null);

        var adapter = new SageCustomerAdapter(client.Object);

        var customer = await adapter.GetCustomerAsync("missing", CancellationToken.None);

        customer.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomerAsync_WhenNotFound_ThrowsNotFound()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        client.Setup(c => c.GetCustomerAsync("missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageEntityNotFoundException("Customer missing."));

        var adapter = new SageCustomerAdapter(client.Object);

        var act = async () => await adapter.GetCustomerAsync("missing", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCustomerAsync_WhenSdkFails_ThrowsDomainException()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        client.Setup(c => c.GetCustomerAsync("100", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageSdkException("Boom"));

        var adapter = new SageCustomerAdapter(client.Object);

        var act = async () => await adapter.GetCustomerAsync("100", CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
