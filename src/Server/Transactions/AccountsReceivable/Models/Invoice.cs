namespace Server.Transactions.AccountsReceivable.Models;

public record Invoice(string Id, string CustomerId, decimal Amount, DateTime IssuedOn, string Status);
