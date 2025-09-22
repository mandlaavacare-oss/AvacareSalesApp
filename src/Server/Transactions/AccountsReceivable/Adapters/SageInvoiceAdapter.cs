using Server.Transactions.AccountsReceivable.Models;
using Server.Sage;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SageInvoiceAdapter : IInvoiceAdapter
{
    public Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(SageSdkStub.SaveInvoice(invoice));
    }
}
