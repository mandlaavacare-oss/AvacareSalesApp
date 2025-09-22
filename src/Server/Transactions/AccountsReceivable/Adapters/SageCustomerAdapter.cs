using Server.Transactions.AccountsReceivable.Models;
using Server.Sage;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SageCustomerAdapter : ICustomerAdapter
{
    public Task<Customer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(SageSdkStub.FindCustomer(customerId));
    }
}
