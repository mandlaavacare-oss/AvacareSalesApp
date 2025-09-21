using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SageCustomerAdapter : ICustomerAdapter
{
    public Task<Customer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Integrate customer retrieval with Sage SDK.");
    }
}
