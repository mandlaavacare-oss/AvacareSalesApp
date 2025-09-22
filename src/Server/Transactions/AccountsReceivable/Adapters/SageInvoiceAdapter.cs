using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Sdk;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SageInvoiceAdapter : IInvoiceAdapter
{
    private readonly ISageAccountsReceivableClient _client;

    public SageInvoiceAdapter(ISageAccountsReceivableClient client)
    {
        _client = client;
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        try
        {
            var draft = new SageInvoiceDraft(invoice.CustomerId, invoice.Amount, invoice.IssuedOn, invoice.Id, invoice.Status);
            var sdkInvoice = await _client.CreateInvoiceAsync(draft, cancellationToken);

            return new Invoice(sdkInvoice.DocumentNumber, sdkInvoice.CustomerCode, sdkInvoice.Amount, sdkInvoice.IssuedOn, sdkInvoice.Status);
        }
        catch (SageEntityNotFoundException ex)
        {
            throw new NotFoundException($"Customer {invoice.CustomerId} was not found in Sage.", ex);
        }
        catch (SageSdkException ex)
        {
            throw new DomainException("Unable to create invoice in Sage.", ex);
        }
    }
}
