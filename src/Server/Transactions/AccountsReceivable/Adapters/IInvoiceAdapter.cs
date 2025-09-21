using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Adapters;

public interface IInvoiceAdapter
{
    Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken);
}
