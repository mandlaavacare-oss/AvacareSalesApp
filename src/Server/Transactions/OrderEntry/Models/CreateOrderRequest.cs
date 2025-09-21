namespace Server.Transactions.OrderEntry.Models;

public record CreateOrderRequest(string CustomerId, DateTime OrderDate, IReadOnlyCollection<SalesOrderLine> Lines);
