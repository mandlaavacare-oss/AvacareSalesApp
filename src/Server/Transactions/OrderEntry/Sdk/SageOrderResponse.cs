namespace Server.Transactions.OrderEntry.Sdk;

public record SageOrderResponse(string OrderNumber, SageOrderStatus Status, IReadOnlyCollection<SageOrderLine> Lines);
