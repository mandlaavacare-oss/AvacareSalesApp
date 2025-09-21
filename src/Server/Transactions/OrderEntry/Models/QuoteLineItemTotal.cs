namespace AvacareSalesApp.Transactions.OrderEntry.Models;

public sealed record QuoteLineItemTotal(string Sku, int Quantity, decimal UnitPrice, decimal LineTotal);
