using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;
using Server.Transactions.AccountsReceivable.Sdk;

namespace Server.Tests.Transactions.AccountsReceivable;

public class CustomerServiceTests
{
    [Fact]
    public async Task GetCustomerAsync_ReturnsCustomer()
    {
        var adapter = new Mock<ICustomerAdapter>();
        var logger = Mock.Of<ILogger<CustomerService>>();
        var expected = new Customer("100", "Acme", "info@acme.test", 1000m);
        adapter.Setup(a => a.GetCustomerAsync("100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new CustomerService(adapter.Object, logger);

        var customer = await service.GetCustomerAsync("100", CancellationToken.None);

        customer.Should().Be(expected);
    }

    [Fact]
    public async Task GetCustomerAsync_WhenMissing_ThrowsNotFound()
    {
        var adapter = new Mock<ICustomerAdapter>();
        var logger = Mock.Of<ILogger<CustomerService>>();
        adapter.Setup(a => a.GetCustomerAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var service = new CustomerService(adapter.Object, logger);

        var act = async () => await service.GetCustomerAsync("missing", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCustomerAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<ICustomerAdapter>();
        var logger = new Mock<ILogger<CustomerService>>();
        adapter.Setup(a => a.GetCustomerAsync("100", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var service = new CustomerService(adapter.Object, logger.Object);

        var act = async () => await service.GetCustomerAsync("100", CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task GetCustomerAsync_WithSageAdapter_ReturnsMappedCustomer()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        var logger = Mock.Of<ILogger<CustomerService>>();
        client.Setup(c => c.GetCustomerAsync("100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SageCustomer("100", "Acme", "info@acme.test", 1000m));

        var adapter = new SageCustomerAdapter(client.Object);
        var service = new CustomerService(adapter, logger);

        var customer = await service.GetCustomerAsync("100", CancellationToken.None);

        customer.Should().Be(new Customer("100", "Acme", "info@acme.test", 1000m));
    }

    [Fact]
    public async Task GetCustomerAsync_WithSageAdapterWhenMissing_ThrowsNotFound()
    {
        var client = new Mock<ISageAccountsReceivableClient>();
        var logger = Mock.Of<ILogger<CustomerService>>();
        client.Setup(c => c.GetCustomerAsync("missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageEntityNotFoundException("Customer missing."));

        var adapter = new SageCustomerAdapter(client.Object);
        var service = new CustomerService(adapter, logger);

        var act = async () => await service.GetCustomerAsync("missing", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
