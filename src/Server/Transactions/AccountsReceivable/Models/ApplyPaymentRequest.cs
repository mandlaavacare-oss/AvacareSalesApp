namespace Server.Transactions.AccountsReceivable.Models;

public record ApplyPaymentRequest(string InvoiceId, decimal Amount, DateTime PaidOn);
