namespace Server.Transactions.OrderEntry.Models;

public record SalesOrder(string Id, string CustomerId, DateTime OrderDate, IReadOnlyCollection<SalesOrderLine> Lines);
