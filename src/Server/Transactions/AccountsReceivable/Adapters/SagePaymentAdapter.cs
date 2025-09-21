using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SagePaymentAdapter : IPaymentAdapter
{
    public Task<Payment> ApplyPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Integrate payment posting with Sage SDK.");
    }
}
