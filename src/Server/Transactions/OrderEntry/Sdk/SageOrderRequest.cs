namespace Server.Transactions.OrderEntry.Sdk;

public record SageOrderRequest(string CustomerId, DateTime OrderDate, IReadOnlyCollection<SageOrderLine> Lines);
