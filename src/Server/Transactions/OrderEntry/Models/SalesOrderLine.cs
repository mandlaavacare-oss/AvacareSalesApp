namespace Server.Transactions.OrderEntry.Models;

public record SalesOrderLine(string ProductId, decimal Quantity, decimal UnitPrice);
