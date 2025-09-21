using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Services;

public interface ICustomerService
{
    Task<Customer> GetCustomerAsync(string customerId, CancellationToken cancellationToken);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerAdapter _adapter;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerAdapter adapter, ILogger<CustomerService> logger)
    {
        _adapter = adapter;
        _logger = logger;
    }

    public async Task<Customer> GetCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _adapter.GetCustomerAsync(customerId, cancellationToken);
            if (customer is null)
            {
                throw new NotFoundException($"Customer {customerId} was not found in Sage.");
            }

            return customer;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load customer {CustomerId}", customerId);
            throw new DomainException("Unable to retrieve customer from Sage.", ex);
        }
    }
}
