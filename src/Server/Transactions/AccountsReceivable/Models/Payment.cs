namespace Server.Transactions.AccountsReceivable.Models;

public record Payment(string Id, string InvoiceId, decimal Amount, DateTime PaidOn);
