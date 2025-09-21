namespace Server.Transactions.Inventory.Adapters;

public record SageInventoryProduct(
    string StockCode,
    string Name,
    string Description,
    decimal UnitPrice,
    int QuantityOnHand);
