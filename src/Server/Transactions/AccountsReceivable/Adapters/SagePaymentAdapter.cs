using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Sdk;

namespace Server.Transactions.AccountsReceivable.Adapters;

public class SagePaymentAdapter : IPaymentAdapter
{
    private readonly ISageAccountsReceivableClient _client;

    public SagePaymentAdapter(ISageAccountsReceivableClient client)
    {
        _client = client;
    }

    public async Task<Payment> ApplyPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            var draft = new SagePaymentDraft(payment.InvoiceId, payment.Amount, payment.PaidOn, payment.Id);
            var sdkPayment = await _client.ApplyPaymentAsync(draft, cancellationToken);

            return new Payment(sdkPayment.ReceiptNumber, sdkPayment.InvoiceDocumentNumber, sdkPayment.Amount, sdkPayment.PaidOn);
        }
        catch (SageEntityNotFoundException ex)
        {
            throw new NotFoundException($"Invoice {payment.InvoiceId} was not found in Sage.", ex);
        }
        catch (SageSdkException ex)
        {
            throw new DomainException("Unable to post payment in Sage.", ex);
        }
    }
}
