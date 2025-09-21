using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Adapters;

public interface IPaymentAdapter
{
    Task<Payment> ApplyPaymentAsync(Payment payment, CancellationToken cancellationToken);
}
