using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Adapters;

public interface ICustomerAdapter
{
    Task<Customer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken);
}
