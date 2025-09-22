using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Sdk;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SageCustomerAdapter : ICustomerAdapter
{
    private readonly ISageAccountsReceivableClient _client;

    public SageCustomerAdapter(ISageAccountsReceivableClient client)
    {
        _client = client;
    }

    public Task<Customer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        return GetCustomerInternalAsync(customerId, cancellationToken);
    }

    private async Task<Customer?> GetCustomerInternalAsync(string customerId, CancellationToken cancellationToken)
    {
        try
        {
            var sdkCustomer = await _client.GetCustomerAsync(customerId, cancellationToken);
            if (sdkCustomer is null)
            {
                return null;
            }

            return new Customer(sdkCustomer.AccountCode, sdkCustomer.Name, sdkCustomer.EmailAddress, sdkCustomer.CreditLimit);
        }
        catch (SageEntityNotFoundException ex)
        {
            throw new NotFoundException($"Customer {customerId} was not found in Sage.", ex);
        }
        catch (SageSdkException ex)
        {
            throw new DomainException("Unable to retrieve customer from Sage.", ex);
        }
    }
}
