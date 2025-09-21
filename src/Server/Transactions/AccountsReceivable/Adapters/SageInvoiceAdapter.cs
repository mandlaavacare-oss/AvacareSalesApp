using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SageInvoiceAdapter : IInvoiceAdapter
{
    public Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Integrate invoice creation with Sage SDK.");
    }
}
