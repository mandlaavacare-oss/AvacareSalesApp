namespace Server.Transactions.AccountsReceivable.Sdk;

public interface ISageAccountsReceivableClient
{
    Task<SageCustomer?> GetCustomerAsync(string customerCode, CancellationToken cancellationToken);

    Task<SageInvoice> CreateInvoiceAsync(SageInvoiceDraft invoice, CancellationToken cancellationToken);

    Task<SagePayment> ApplyPaymentAsync(SagePaymentDraft payment, CancellationToken cancellationToken);
}

public record SageCustomer(string AccountCode, string Name, string EmailAddress, decimal CreditLimit);

public record SageInvoiceDraft(string CustomerCode, decimal Amount, DateTime IssuedOn, string ExternalReference, string Status);

public record SageInvoice(string DocumentNumber, string CustomerCode, decimal Amount, DateTime IssuedOn, string Status);

public record SagePaymentDraft(string InvoiceDocumentNumber, decimal Amount, DateTime PaidOn, string ExternalReference);

public record SagePayment(string ReceiptNumber, string InvoiceDocumentNumber, decimal Amount, DateTime PaidOn, string Status);
