namespace Server.Transactions.Inventory.Sdk;

public record SageInventoryItem(string Code, string Name, string Description, decimal UnitPrice, int QuantityOnHand);
