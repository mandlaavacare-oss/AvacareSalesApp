namespace Server.Transactions.AccountsReceivable.Models;

public record CreateInvoiceRequest(string CustomerId, decimal Amount, DateTime IssuedOn, string Status);
