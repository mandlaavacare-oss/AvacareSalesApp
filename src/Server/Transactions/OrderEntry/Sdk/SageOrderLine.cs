namespace Server.Transactions.OrderEntry.Sdk;

public record SageOrderLine(string ProductCode, decimal Quantity, decimal UnitPrice);
