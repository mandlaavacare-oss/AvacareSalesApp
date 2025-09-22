using Server.Transactions.AccountsReceivable.Models;
using Server.Sage;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SagePaymentAdapter : IPaymentAdapter
{
    public Task<Payment> ApplyPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(SageSdkStub.SavePayment(payment));
    }
}
